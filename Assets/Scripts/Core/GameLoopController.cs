using System.Collections.Generic;
using NightCafe.Audio;
using NightCafe.Config;
using NightCafe.Gameplay;
using NightCafe.InputLayer;
using NightCafe.Services;
using NightCafe.UI;
using UnityEngine;

namespace NightCafe.Core
{
    /// <summary>
    /// Scene root: builds the services, wires them together and runs the
    /// Title -> Playing -> Breather -> GameOver state machine, plus the attract demo
    /// that plays on an idle title screen. This is the only place that decides what a
    /// catch or a miss means.
    /// </summary>
    public sealed class GameLoopController : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Indexed by GameMode: A, B.")]
        [SerializeField] ModeConfig[] modeConfigs = new ModeConfig[GameModeExtensions.Count];
        [SerializeField] LaneConfig laneConfig;
        [SerializeField] DeviceConfig deviceConfig;
        [SerializeField] AudioConfig audioConfig;

        [Header("Scene references")]
        [SerializeField] LaneSpawner spawner;
        [SerializeField] LaneInput laneInput;
        [SerializeField] PlayerPositionController barista;
        [SerializeField] HudView hud;
        [SerializeField] TimedSpriteFx[] brokenCupFx;

        [Header("Presentation")]
        [SerializeField] DeviceShellView deviceShell;
        [SerializeField] StainStripView stainStrip;
        [SerializeField] CatCrossingView cat;
        [SerializeField] OrderPanelView orderPanel;
        [SerializeField] ScreenStyleApplier styleApplier;
        [SerializeField] ScreenStyle artStyle;
        [SerializeField] PauseMenuView menu;
        [Tooltip("The two footstools: left (under LeftUp), right (under RightUp).")]
        [SerializeField] LadderView[] ladderViews = new LadderView[LadderService.Sides];
        [Tooltip("Miro's speech bubble: hints the first times, then jokes (review 2026-09-26).")]
        [SerializeField] SpeechBubbleView bubble;
        [Tooltip("Noir, the black cat who bumps the stools (review 2026-09-26).")]
        [SerializeField] BlackCatView blackCat;
        [Tooltip("Steam and shaking for the terrible ten seconds (level 5+).")]
        [SerializeField] SteamFx steam;
        [Tooltip("Seconds between the wild machine's shrieks while it lasts.")]
        [SerializeField] float frenzyShriekEvery = 2.2f;
        [SerializeField] FlashFx neonFlash;
        [SerializeField] FlashFx screenDim;
        [SerializeField] SpriteSequenceFx neonCat;
        [SerializeField] ClockWidget clock;
        [SerializeField] TitleToggleView titleToggles;
        [SerializeField] AudioService audioService;
        [SerializeField] LcdPointer pointer;

        [Header("Platform")]
        [SerializeField] int targetFrameRate = 60;
        [SerializeField] float gameOverDimAlpha = 0.55f;

        [Header("A missed cup falls to the floor, then breaks (review 2026-09-26)")]
        [Tooltip("LCD units per second squared.")]
        [SerializeField] float cupFallGravity = 30f;
        [Tooltip("Sideways speed the cup keeps from its slide as it leaves the rail end.")]
        [SerializeField] float cupFallCarry = 1.5f;
        [SerializeField] float cupFallSpin = 420f;

        [Header("Café events (1.1.0)")]
        [Tooltip("How far below his slot Miro starts climbing an askew ladder, in LCD units.")]
        [SerializeField] float climbHeight = 1.0f;
        [SerializeField] float levelBannerSeconds = 1.8f;
        [Tooltip("Seconds a speech bubble line stays up; a two-line hint gets longer.")]
        [SerializeField] float bubbleSeconds = 2.4f;
        [SerializeField] float hintBubbleSeconds = 3.4f;
        [Tooltip("Paprika (ginger) and Noir (black) relative to Sablé's after-shift size.")]
        [SerializeField] float strayScale = 0.8f;
        [SerializeField] float bumperScale = 0.65f;

        readonly List<PilotCup> _pilotCups = new(8);
        readonly List<string> _unlocks = new(2);

        // Mode-independent
        SettingsService _settings;
        ProfileService _profile;
        HapticsService _haptics;
        BrewTimer _brew;
        CatCueService _catCue;
        AttractPilot _pilot;

        // Rebuilt whenever the lever flips
        GameMode _mode;
        ModeConfig _config;
        TempoService _tempo;
        ScoreService _score;
        PenaltyService _penalty;
        OrderService _orders;

        RoundStateMachine _flow;
        bool _rolledOver;
        bool _menuPausedRound; // the open menu is holding a live shift
        float _lastMopX = float.NegativeInfinity;

        // Levels and café events (rebuilt with the mode, like the balance services)
        readonly IRandom _eventRng = new UnityRandom();
        readonly LadderChange[] _ladderChanges = new LadderChange[LadderService.Sides];
        ShiftEventDirector _events;
        LadderService _ladders;
        QuietSpellDirector _quiet;
        BaristaQuips _quips;
        int _level;
        bool _rush;
        float _rushLeft;
        bool _frenzy;           // the terrible ten seconds: one machine gone haywire
        float _frenzyLeft;
        float _frenzyShriek;
        ShiftEvent _pendingBump;    // Noir is on his way to a stool; the mishap lands with his shove
        int _pendingSide;
        LanePosition? _climbTarget; // Miro is on his way up an askew ladder: in no slot until he arrives
        float _climbLeft;

        public GameState State => _flow.State;

        /// <summary>True while the attract pilot is playing on the title screen (GDD 6).</summary>
        public bool IsDemo => _flow.IsDemo;

        public GameMode Mode => _mode;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            VerifyFxSharesCupSpace();

            _settings = new SettingsService(new PlayerPrefsSettingsStore(), Loc.FromSystem(Application.systemLanguage));
            Loc.Set(_settings.Language);
            _profile = new ProfileService(FileProfileStore.Default());
            _haptics = new HapticsService(HapticsService.CreateBackend(), _settings);
            _brew = new BrewTimer(deviceConfig.brewTimerMaxMinutes);
            if (clock != null)
                clock.Initialise(_brew, () => Time.unscaledTimeAsDouble, deviceConfig.clockHitSize);
            _catCue = new CatCueService(audioConfig.meowChance, new UnityRandom());
            _pilot = new AttractPilot(new UnityRandom(), deviceConfig.pilotReactionSeconds,
                deviceConfig.pilotFumbleChance, LaneConfig.StepsPerLane - 1);
            _flow = new RoundStateMachine(new RoundTimings(
                deviceConfig.attractDelay, deviceConfig.attractMaxDuration,
                deviceConfig.gameOverIdleSeconds, deviceConfig.gameOverRestartLockout));

            _settings.Changed += OnSettingsChanged;
            _profile.Changed += OnProfileChanged;

            spawner.CupReachedCatchPoint += ResolveCup;
            spawner.CupLanded += BreakCup;
            spawner.CupSpawned += PaintCup;

            barista.Initialise(laneConfig);
            audioService.Initialise(_settings);
            titleToggles.Initialise(_settings, _profile);
            cat.CrossingStarted += OnCatCrossingStarted;
            if (blackCat != null)
            {
                blackCat.Bumped += OnNoirBumped;
                blackCat.DecideShove = OnNoirArrived;
            }

            laneInput.Pressed += OnPressed;

            ConfigureMode(_profile.SelectedMode);
            ApplySkin();
            ApplyScreenStyle();
            hud.Relocalize();
        }

        /// <summary>GDD 5.2: the painted diorama, the one screen style since RETRO was dropped.</summary>
        public ScreenStyle EffectiveStyle => artStyle;

        void ApplyScreenStyle()
        {
            if (styleApplier != null)
                styleApplier.Apply(EffectiveStyle);
            if (audioService != null)
                audioService.SetSoundSet(EffectiveStyle != null ? EffectiveStyle.sounds : null);

            // Paprika, the ginger stray, lounges on the stools; Noir bumps them. Without their art
            // Sablé stands in (her after-shift curl on the seat, her walking frames for the rest).
            ScreenStyle style = EffectiveStyle;
            if (style != null)
            {
                float catSize = laneConfig.catScale * style.catScale;
                bool stray = style.strayLounge != null;
                foreach (LadderView view in ladderViews)
                {
                    if (view == null)
                        continue;
                    if (stray)
                        view.SetCat(style.strayLounge, style.strayTrotA, style.strayTrotB, style.strayLeap,
                            catSize * strayScale, catSize * strayScale);
                    else
                        view.SetCat(style.catAsleepA, style.catA, style.catB, null, catSize * 0.55f, catSize * 0.8f);
                    view.SetBroken(style.stepBroken);
                }

                if (blackCat != null)
                {
                    bool noir = style.bumperWalkA != null;
                    blackCat.SetFrames(noir ? style.bumperWalkA : style.catA, noir ? style.bumperWalkB : style.catB,
                        noir ? style.bumperBump : style.catA, noir ? style.bumperRear : null,
                        noir ? style.bumperTailA : null, noir ? style.bumperTailB : null, catSize * bumperScale);
                }
            }

            // The applier parks Miro back on his lane slot; on the title he belongs at the counter.
            if (_flow != null && State == GameState.Title)
                PresentTitleBarista();
        }

        void Start()
        {
            // Stage 3: signs in to Play Games in the background if the player already uses it.
            if (OnlineScores.Backend.Available)
                OnlineScores.Backend.SignInSilently();
            EnterTitle();
        }

        /// <summary>
        /// ResolveCup hands a cup's localPosition straight to TimedSpriteFx.Show, which writes it
        /// back as a localPosition. That is only correct while both roots share one parent space -
        /// otherwise broken cups quietly appear in the wrong place, with no error anywhere.
        /// </summary>
        void VerifyFxSharesCupSpace()
        {
            if (brokenCupFx == null || brokenCupFx.Length == 0 || spawner.CupRoot == null)
                return;

            Transform fxRoot = brokenCupFx[0].transform.parent;
            if (fxRoot == null)
                return;

            bool sameParent = fxRoot.parent == spawner.CupRoot.parent;
            bool identity = fxRoot.localPosition == Vector3.zero
                            && spawner.CupRoot.localPosition == Vector3.zero
                            && fxRoot.localScale == Vector3.one
                            && spawner.CupRoot.localScale == Vector3.one;

            if (!sameParent || !identity)
                Debug.LogError("[NightCafe] CupPoolRoot and FX must be identity siblings, " +
                               "otherwise broken cups land away from the cup that broke.");
        }

        void OnDestroy()
        {
            DetachModeServices();

            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;

            if (_profile != null)
                _profile.Changed -= OnProfileChanged;

            if (spawner != null)
            {
                spawner.CupReachedCatchPoint -= ResolveCup;
                spawner.CupLanded -= BreakCup;
                spawner.CupSpawned -= PaintCup;
            }

            Time.timeScale = 1f; // never leave the editor frozen behind a menu

            if (cat != null)
                cat.CrossingStarted -= OnCatCrossingStarted;
            if (blackCat != null)
                blackCat.Bumped -= OnNoirBumped;

            if (laneInput != null)
                laneInput.Pressed -= OnPressed;
        }

        // ------------------------------------------------------------------ modes

        /// <summary>
        /// Every balance service is built from the mode's config, so flipping the lever means
        /// rebuilding them rather than poking numbers into live objects.
        /// </summary>
        void ConfigureMode(GameMode mode)
        {
            DetachModeServices();

            _mode = mode;
            _config = modeConfigs[(int)mode];

            _tempo = new TempoService(_config.Tempo);
            _score = new ScoreService(_config.Score);
            _penalty = new PenaltyService(_config.maxStains);
            _orders = new OrderService(_config.Orders, new UnityRandom());
            _events = new ShiftEventDirector(_config.Events, _eventRng);
            _ladders = new LadderService(_config.Events);
            _quiet = new QuietSpellDirector(_config.Events, _eventRng);
            _quips ??= new BaristaQuips(_eventRng);
            spawner.LaneClosed = lane => _ladders.LaneClosed(lane);

            _score.ScoreChanged += hud.SetScore;
            _score.ScoreChanged += OnScoreChanged;
            _score.MercyTriggered += OnMercyTriggered;
            _score.BreatherTriggered += OnBreatherTriggered;
            _score.ComboBonusAwarded += OnComboBonusAwarded;
            _score.RolloverOccurred += OnRolloverOccurred;
            _penalty.StainsChanged += stainStrip.SetStains;
            _penalty.GameOverTriggered += OnGameOverTriggered;
            _orders.OrderChanged += OnOrderChanged;

            spawner.Initialise(_config, laneConfig, _tempo, new UnityRandom());

            hud.SetMode(mode);
            hud.SetBest(_profile.Best(mode));
            deviceShell.SetMode(mode);
        }

        void DetachModeServices()
        {
            if (_score != null)
            {
                _score.ScoreChanged -= hud.SetScore;
                _score.ScoreChanged -= OnScoreChanged;
                _score.MercyTriggered -= OnMercyTriggered;
                _score.BreatherTriggered -= OnBreatherTriggered;
                _score.ComboBonusAwarded -= OnComboBonusAwarded;
                _score.RolloverOccurred -= OnRolloverOccurred;
            }

            if (_penalty != null)
            {
                _penalty.StainsChanged -= stainStrip.SetStains;
                _penalty.GameOverTriggered -= OnGameOverTriggered;
            }

            if (_orders != null)
                _orders.OrderChanged -= OnOrderChanged;
        }

        void FlipMode()
        {
            GameMode next = _mode.Next();
            _profile.SelectMode(next);
            ConfigureMode(next);
            ResetRoundState();
            audioService.Play(GameSfx.LeverClick);
            _haptics.OneShot(audioConfig.catchHapticMs);
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            float dt = Time.deltaTime;

            // The brew timer is a kitchen gadget, not a game system: it rings in any state.
            if (_brew != null && _brew.Tick(Time.unscaledTimeAsDouble))
            {
                audioService.Play(GameSfx.BrewAlarm);
                _haptics.Pattern(HapticPatterns.Pulses(
                    audioConfig.gameOverHapticPulses, audioConfig.gameOverHapticMs, audioConfig.gameOverHapticGapMs));
            }

            RoundTransition transition = _flow.Tick(Time.time);
            if (transition != RoundTransition.None)
            {
                Apply(transition);
                return;
            }

            if (State is GameState.Playing or GameState.Breather)
            {
                _orders.Tick(dt);
                SweepBrokenCups();
                if (IsDemo)
                    UpdateDemo(dt);
                else
                    UpdateShift(dt);
            }
        }

        /// <summary>Carries out what the flow decided; the flow is told once the state is really in place.</summary>
        void Apply(RoundTransition transition)
        {
            switch (transition)
            {
                case RoundTransition.EnterTitle:
                    EnterTitle();
                    break;
                case RoundTransition.EnterDemo:
                    EnterDemo();
                    break;
                case RoundTransition.StartRound:
                    StartRound();
                    break;
                case RoundTransition.EnterGameOver:
                    EnterGameOver();
                    break;
                case RoundTransition.ResumePlaying:
                    _flow.ResumedPlaying();
                    spawner.ResumeAfterBreather();
                    break;
            }
        }

        void UpdateDemo(float dt)
        {
            _pilotCups.Clear();
            IReadOnlyList<CupController> cups = spawner.ActiveCups;
            for (int i = 0; i < cups.Count; i++)
            {
                if (!cups[i].IsFalling) // already missed, nothing left to decide
                    _pilotCups.Add(new PilotCup(cups[i].Serial, cups[i].Lane, cups[i].StepIndex, _orders.IsWanted(cups[i].Colour)));
            }

            int lane = _pilot.Decide(_pilotCups, (int)barista.Current, dt);
            if (lane < 0)
                return;

            var position = (LanePosition)lane;
            deviceShell.Press(position);
            barista.MoveTo(position);
        }

        // ------------------------------------------------------------------ input

        /// <summary>
        /// The single entry point for a press. Where it goes depends only on the state, so there
        /// is no "was this tap already consumed" bookkeeping across events to get out of sync.
        /// A touch moves Miro only when it lands on a cap (review 2026-09-26: the old screen
        /// quadrants fired from anywhere on the phone).
        /// </summary>
        void OnPressed(Press press)
        {
            press = ResolveDeviceButton(press, out bool menuButton);

            if (menuButton)
            {
                if (menu.IsOpen)
                    CloseMenu();
                else
                    OpenMenu();
                return;
            }

            if (menu.IsOpen)
            {
                HandleMenuTap(press);
                return;
            }

            if (press.Lane.HasValue)
                deviceShell.Press(press.Lane.Value);

            // On the result screen the lever still flips the mode, and takes you to the title in it.
            if (State == GameState.GameOver && LeverPressed(press))
            {
                FlipMode();
                Apply(RoundTransition.EnterTitle);
                return;
            }

            // The toggles hide during the demo, but the lever is still there to be flipped.
            bool consumed = (_flow.TitleUiActive || _flow.IsDemo) && TitleScreenConsumed(press);
            Apply(_flow.Press(Time.time, consumed));

            if (_flow.AcceptsLaneMoves && press.Lane.HasValue)
                MoveBarista(press.Lane.Value);
        }

        /// <summary>
        /// A lane press in a live shift. The lower slots are always a plain move; an upper slot
        /// asks its ladder first (1.1.0): a cat to shoo, a stool to set up, a slow climb, or
        /// nothing to stand on.
        /// </summary>
        void MoveBarista(LanePosition lane)
        {
            _climbTarget = null; // a new press changes his mind mid-climb
            if (!lane.IsUp())
            {
                barista.MoveTo(lane);
                return;
            }

            int side = LadderService.SideOf(lane);
            LadderPress press = _ladders.Press(side);
            switch (press)
            {
                case LadderPress.Move:
                    barista.MoveTo(lane);
                    break;
                case LadderPress.Climb:
                    if (barista.Current == lane)
                    {
                        barista.MoveTo(lane);
                        break;
                    }

                    _climbTarget = lane;
                    _climbLeft = _ladders.ClimbDelay;
                    barista.BeginClimb(lane, _ladders.ClimbDelay, climbHeight);
                    audioService.Play(GameSfx.LadderCreak);
                    break;
                case LadderPress.Blocked:
                    _haptics.OneShot(audioConfig.catchHapticMs);
                    barista.Shrug();
                    Say(Quip.LadderBlocked);
                    break;
                case LadderPress.Shoo:
                    // He steps to the foot of the stool and shoos her off, outwards.
                    GoToFootOf(side);
                    barista.Shoo(Outward(side));
                    ladderViews[side].ShooCat();
                    audioService.Play(GameSfx.CatHiss);
                    RenderLadder(side);
                    Say(Quip.CatShooed);
                    break;
                case LadderPress.RepairStep:
                case LadderPress.Repaired:
                    GoToFootOf(side);
                    barista.Lift(-Outward(side)); // the stool lies inwards of him
                    audioService.Play(GameSfx.LadderKnock);
                    RenderLadder(side);
                    if (press == LadderPress.Repaired)
                        Say(Quip.LadderRepaired);
                    break;
            }
        }

        /// <summary>Shooing and lifting are done from the floor next to the stool: that side's lower slot.</summary>
        void GoToFootOf(int side)
        {
            LanePosition foot = LadderService.LowerLane(side);
            if (barista.Current != foot)
                barista.MoveTo(foot);
        }

        /// <summary>-1 for the left stool, +1 for the right: where a knocked-over stool lies and a shooed cat flies.</summary>
        static float Outward(int side) => side == 0 ? -1f : 1f;

        /// <summary>Miro says something about the moment - in a live shift only, not in the demo.</summary>
        void Say(Quip quip)
        {
            if (bubble == null || _flow.IsDemo || State is not (GameState.Playing or GameState.Breather))
                return;

            string line = _quips.Line(quip);
            bubble.Say(line, line.Contains("\n") ? hintBubbleSeconds : bubbleSeconds);
        }

        // ------------------------------------------------------------------ café events (1.1.0)

        /// <summary>The side whose upper slot Miro stands on, or -1 (down below, or mid-climb).</summary>
        int BaristaUpSide() =>
            !_climbTarget.HasValue && barista.Current.IsUp() ? LadderService.SideOf(barista.Current) : -1;

        /// <summary>Climbs, ladders, the rush clock and the event director, in live play only.</summary>
        void UpdateShift(float dt)
        {
            if (_climbTarget.HasValue)
            {
                _climbLeft -= dt;
                if (_climbLeft <= 0f)
                {
                    barista.MoveTo(_climbTarget.Value);
                    _climbTarget = null;
                }
            }

            _ladders.Tick(dt, BaristaUpSide(), _ladderChanges);
            for (int side = 0; side < LadderService.Sides; side++)
            {
                if (_ladderChanges[side] == LadderChange.None)
                    continue;

                if (_ladderChanges[side] == LadderChange.Broke)
                    LadderBrokeUnderBarista(side);
                else if (_ladderChanges[side] == LadderChange.CatLeft)
                    Say(Quip.CatLeft);
                RenderLadder(side);
            }

            if (_rush)
            {
                _rushLeft -= dt;
                hud.SetRushCountdown(Mathf.Max(0, Mathf.CeilToInt(_rushLeft)));
                if (_rushLeft <= 0f)
                    EndRush();
            }

            if (_frenzy)
                UpdateFrenzy(dt);

            UpdateQuietSpell();

            if (State == GameState.Playing)
                StartEvent(_events.Tick(dt, _level, _ladders.AnyActive || _pendingBump != ShiftEvent.None, _rush,
                    _quiet.Active, BaristaUpSide() >= 0, _frenzy));
        }

        /// <summary>
        /// The terrible ten seconds (level 5+, review 2026-09-26: "some testers miss the thrill"):
        /// one machine, picked at random, steams and shakes and fires its cups back to back at
        /// nearly twice the speed; the other three keep a single cup between them.
        /// </summary>
        void StartFrenzy()
        {
            int lane = PickFrenzyLane();
            if (lane < 0)
                return;

            _frenzy = true;
            _frenzyLeft = _config.frenzySeconds;
            _frenzyShriek = frenzyShriekEvery;
            spawner.StartFrenzy(lane, _config.FrenzyStepFactorAt(_level), _config.frenzyStepGap, _config.frenzyOtherCups);
            _orders.Held = true; // Mode B: the wild machine brews the order, and the order stays put
            if (steam != null)
                steam.Erupt(lane, _config.frenzySeconds);
            audioService.Play(GameSfx.MachineFrenzy);
            _haptics.Pattern(HapticPatterns.Pulses(2, audioConfig.missHapticMs, 60));
            neonFlash.Blink(4, 0.18f);
            hud.SetCountdown(Txt.TerribleTen, Mathf.CeilToInt(_frenzyLeft));
            Say(Quip.FrenzyStart);
        }

        /// <summary>A machine whose lane is open (a broken stool closes its upper lane).</summary>
        int PickFrenzyLane()
        {
            int start = _eventRng.NextInt(0, LanePositionExtensions.Count);
            for (int i = 0; i < LanePositionExtensions.Count; i++)
            {
                int lane = (start + i) % LanePositionExtensions.Count;
                if (!_ladders.LaneClosed(lane))
                    return lane;
            }

            return -1;
        }

        void UpdateFrenzy(float dt)
        {
            _frenzyLeft -= dt;
            _frenzyShriek -= dt;
            if (_frenzyShriek <= 0f && _frenzyLeft > 1f)
            {
                _frenzyShriek = frenzyShriekEvery;
                audioService.Play(GameSfx.MachineFrenzy);
            }

            hud.SetCountdown(Txt.TerribleTen, Mathf.Max(0, Mathf.CeilToInt(_frenzyLeft)));
            if (_frenzyLeft <= 0f)
                EndFrenzy();
        }

        void EndFrenzy()
        {
            if (!_frenzy)
                return;

            Say(Quip.FrenzyEnd); // quiet after the shift: Say only talks in live play
            _frenzy = false;
            spawner.StopFrenzy();
            _orders.Held = false;
            if (steam != null)
                steam.Stop();
            hud.SetCountdown(Txt.TerribleTen, -1);
            _events.FrenzyEnded(_level);
        }

        /// <summary>From level 5 on, now and then a few calmer points: slower cups, one fewer on screen.</summary>
        void UpdateQuietSpell()
        {
            switch (_quiet.Tick(_score.TotalScore, _level, _rush || _frenzy))
            {
                case QuietChange.Started:
                    _tempo.StepFactor = _config.quietStepFactor;
                    spawner.ExtraCups = _config.quietExtraCups;
                    hud.FlashBanner(Loc.T(Txt.QuietSpell), levelBannerSeconds);
                    Say(Quip.QuietStart);
                    break;
                case QuietChange.Ended:
                    _tempo.StepFactor = 1f;
                    spawner.ExtraCups = 0;
                    hud.FlashBanner(Loc.T(Txt.FullSpeed), levelBannerSeconds);
                    audioService.Play(GameSfx.LevelUp);
                    Say(Quip.QuietEnd);
                    break;
            }
        }

        void StartEvent(ShiftEvent shiftEvent)
        {
            if (shiftEvent == ShiftEvent.None)
                return;

            if (shiftEvent == ShiftEvent.RushHour)
            {
                StartRush();
                return;
            }

            if (shiftEvent == ShiftEvent.MachineFrenzy)
            {
                StartFrenzy();
                return;
            }

            int side = LadderService.PickSide(shiftEvent, BaristaUpSide(), _eventRng);
            if (_ladders.State(side) != LadderState.Standing)
                return;

            // The ladder mishaps are Noir's doing: he walks up to the stool first, and the mishap
            // lands with his shove (OnNoirBumped). Paprika just jumps up.
            if (shiftEvent != ShiftEvent.CatOnLadder && blackCat != null)
            {
                _pendingBump = shiftEvent;
                _pendingSide = side;
                blackCat.Visit(laneConfig.GetBaristaSlot(LadderService.UpperLane(side)).x, Outward(side));
                return;
            }

            ApplyLadderEvent(shiftEvent, side);
        }

        /// <summary>
        /// Noir's shove: a shake if Miro is up on that stool now (it only ever shakes under him),
        /// otherwise the planned mishap (a shake aimed at him that finds the stool empty knocks it askew).
        /// </summary>
        /// <summary>
        /// Noir has reached the stool: what he does depends on where Miro stands now - the tail
        /// for a stool with Miro on it (it shakes), his hind legs to push one over, a shoulder
        /// to knock one askew.
        /// </summary>
        NoirShove OnNoirArrived()
        {
            if (_pendingBump == ShiftEvent.None)
                return NoirShove.Shoulder;

            _pendingBump = LadderService.BumpOutcome(_pendingBump, _pendingSide, BaristaUpSide());
            return _pendingBump switch
            {
                ShiftEvent.LadderCreak => NoirShove.Tail,
                ShiftEvent.LadderRepair => NoirShove.Rear,
                _ => NoirShove.Shoulder
            };
        }

        /// <summary>The blow lands: the mishap decided on arrival happens now.</summary>
        void OnNoirBumped()
        {
            if (_pendingBump == ShiftEvent.None)
                return;

            ShiftEvent mishap = _pendingBump;
            _pendingBump = ShiftEvent.None;
            ApplyLadderEvent(mishap, _pendingSide);
        }

        void ApplyLadderEvent(ShiftEvent shiftEvent, int side)
        {
            if (!_ladders.StartOn(side, shiftEvent))
                return;

            audioService.Play(shiftEvent switch
            {
                ShiftEvent.CatOnLadder => GameSfx.CatMeow,
                ShiftEvent.LadderCreak => GameSfx.LadderCreak,
                ShiftEvent.LadderRepair => GameSfx.LadderBreak,
                _ => GameSfx.LadderKnock
            });
            Say(shiftEvent switch
            {
                ShiftEvent.CatOnLadder => Quip.CatOnLadder,
                ShiftEvent.LadderCreak => Quip.LadderCreak,
                ShiftEvent.LadderSlowClimb => Quip.LadderSlowClimb,
                _ => Quip.LadderToppled
            });

            // A climb already headed to a stool that is now blocked has nowhere to arrive.
            if (_climbTarget.HasValue && LadderService.SideOf(_climbTarget.Value) == side && _ladders.Blocks(side))
            {
                _climbTarget = null;
                barista.MoveTo(barista.Current);
            }

            RenderLadder(side);
        }

        /// <summary>A creaking ladder gave way: Miro drops to the counter below and the cup on his tray breaks.</summary>
        void LadderBrokeUnderBarista(int side)
        {
            audioService.Play(GameSfx.LadderBreak);
            if (BaristaUpSide() != side)
                return;

            barista.Fall(LadderService.LowerLane(side), Outward(side));
            Say(Quip.LadderBroke);
            _score.RegisterMiss();
            ShowBrokenCup(new Vector2(barista.transform.localPosition.x, laneConfig.barLineY));
            _haptics.OneShot(audioConfig.missHapticMs);
            cat.Play();
            _penalty.AddStain();
        }

        void StartRush()
        {
            _rush = true;
            _rushLeft = _config.rushHourSeconds;
            _tempo.StepFactor = _config.rushStepFactor;
            spawner.ExtraCups = _config.rushExtraCups;
            audioService.Play(GameSfx.RushBell);
            audioService.SetRushHour(true);
            neonFlash.Blink(3, 0.25f);
            hud.SetRushCountdown(Mathf.CeilToInt(_rushLeft));
            Say(Quip.RushStart);
        }

        void EndRush()
        {
            if (!_rush)
                return;

            Say(Quip.RushEnd); // quiet after the shift: Say only talks in live play

            _rush = false;
            _tempo.StepFactor = 1f;
            spawner.ExtraCups = 0;
            audioService.SetRushHour(false);
            hud.SetRushCountdown(-1);
            _events.RushEnded(_level);
        }

        void OnScoreChanged(int _)
        {
            if (State == GameState.Title || _level <= 0)
                return;

            int level = _config.Events.LevelFor(_score.TotalScore);
            if (level <= _level)
                return;

            _level = level;
            hud.SetLevel(level);
            hud.FlashBanner(Loc.F(Txt.LevelBanner, level), levelBannerSeconds);
            audioService.Play(GameSfx.LevelUp);
        }

        void RenderLadder(int side)
        {
            if (side < ladderViews.Length && ladderViews[side] != null)
                ladderViews[side].Show(_ladders.State(side), _ladders.RepairsLeft(side), _config.ladderRepairPresses);
        }

        /// <summary>Everything back to a quiet café: no rush, ladders up, no climb.</summary>
        void ResetShiftEvents()
        {
            EndRush();
            EndFrenzy();
            if (steam != null)
                steam.Clear();
            _events.Reset();
            _ladders.Reset();
            _quiet.Reset();
            _quips.Reset();
            _pendingBump = ShiftEvent.None;
            if (blackCat != null)
                blackCat.Hide();
            _tempo.StepFactor = 1f;
            spawner.ExtraCups = 0;
            if (bubble != null)
                bubble.Hide();
            _climbTarget = null;
            foreach (LadderView view in ladderViews)
            {
                if (view != null)
                    view.ResetAll();
            }

            hud.ClearBanner();
        }

        /// <summary>
        /// Maps a touch onto the device: the MENU pill, a lane cap, or nothing. Keyboard presses
        /// already know their lane (or that they are the back button).
        /// </summary>
        Press ResolveDeviceButton(Press press, out bool menuButton)
        {
            menuButton = press.Menu;
            if (!press.HasScreenPosition || pointer == null)
                return press;

            Ray ray = pointer.ScreenRay(press.ScreenPosition);
            if (deviceShell.MenuHit(ray))
            {
                deviceShell.PressMenu();
                menuButton = true;
                return press;
            }

            return deviceShell.TryCapHit(ray, out LanePosition lane) ? press.WithLane(lane) : press;
        }

        bool LeverPressed(in Press press) =>
            press.HasScreenPosition && pointer != null && deviceShell.LeverHit(pointer.ScreenRay(press.ScreenPosition));

        // ------------------------------------------------------------------ menu

        /// <summary>
        /// Opens the MENU card and freezes the game under it: every system runs on scaled time,
        /// so a zero time scale holds the cups, the flow's timers and the animations exactly
        /// where they were. The attract demo is not a shift to pause; it hands back the title.
        /// </summary>
        void OpenMenu()
        {
            if (_flow.IsDemo)
                EnterTitle();

            _menuPausedRound = State is GameState.Playing or GameState.Breather;
            Time.timeScale = 0f;
            menu.Open(_menuPausedRound, _settings, _mode, OnlineScores.Backend.Available);
            audioService.Play(GameSfx.LeverClick);
        }

        void CloseMenu()
        {
            menu.Close();
            Time.timeScale = 1f;
            _menuPausedRound = false;
            if (State == GameState.Title)
                _flow.EnteredTitle(Time.time); // the attract delay starts over
        }

        void HandleMenuTap(in Press press)
        {
            if (!press.HasScreenPosition || pointer == null || !pointer.TryLcdPoint(press.ScreenPosition, out Vector3 lcdPoint))
                return;

            MenuAction action = menu.Hit(lcdPoint);
            if (action == MenuAction.None)
                return;

            audioService.Play(GameSfx.LeverClick);
            switch (action)
            {
                case MenuAction.Resume:
                    CloseMenu();
                    break;
                case MenuAction.ToggleMusic:
                    _settings.ToggleMusic();
                    break;
                case MenuAction.ToggleSound:
                    _settings.ToggleSfx();
                    break;
                case MenuAction.FlipMode:
                    SwitchModeFromMenu();
                    break;
                case MenuAction.OnlineScores:
                    ShowOnlineScores();
                    return;
                case MenuAction.ToggleLanguage:
                    _settings.ToggleLanguage();
                    break;
                case MenuAction.ShowScores:
                    menu.ShowScores(_mode, _profile.TopScores(_mode));
                    return;
                case MenuAction.Back:
                    menu.ShowMain();
                    break;
                case MenuAction.EndShift:
                    CloseMenu();
                    Apply(_flow.PenaltyGameOver());
                    return;
                case MenuAction.Quit:
                    Quit();
                    return;
            }

            if (menu.IsOpen)
                menu.Render(_settings, _mode);
            titleToggles.Render();
        }

        /// <summary>
        /// A shift belongs to one mode, so switching mid-shift ends it: the score so far still
        /// goes on the list, and the menu stays open over the title in the new mode.
        /// </summary>
        void SwitchModeFromMenu()
        {
            if (_menuPausedRound)
            {
                _profile.SubmitScore(_mode, _score.TotalScore, System.DateTime.Now, out _);
                OnlineScores.Backend.Submit(_mode, _score.TotalScore);
            }

            FlipMode();
            if (State != GameState.Title)
                EnterTitle();

            menu.Open(false, _settings, _mode, OnlineScores.Backend.Available);
            _menuPausedRound = false;
        }

        /// <summary>
        /// Google's leaderboard for the current mode; a player who is not signed in is asked first
        /// (the sign-in sheet and the board both open over the paused game).
        /// </summary>
        void ShowOnlineScores()
        {
            IOnlineScores online = OnlineScores.Backend;
            if (!online.Available)
                return;

            if (online.SignedIn)
            {
                online.ShowBoard(_mode);
                return;
            }

            GameMode mode = _mode;
            online.SignIn(ok =>
            {
                if (ok)
                    online.ShowBoard(mode);
            });
        }

        static void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Android sends the app to the background mid-shift: come back to a paused game, not to three stains.</summary>
        void OnApplicationPause(bool paused)
        {
            if (paused)
                PauseForInterruption();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                PauseForInterruption();
        }

        void PauseForInterruption()
        {
            if (_flow == null || menu == null || menu.IsOpen || _flow.IsDemo)
                return;

            if (State is GameState.Playing or GameState.Breather)
                OpenMenu();
        }

        /// <summary>Toggles and the lever take the tap before it can start a round.</summary>
        bool TitleScreenConsumed(in Press press)
        {
            if (!press.HasScreenPosition || pointer == null)
                return false;

            // On the glass: the tap becomes a point in the LCD scene for the toggles and the clock.
            if (pointer.TryLcdPoint(press.ScreenPosition, out Vector3 lcdPoint))
            {
                if (titleToggles.TryHandleTap(lcdPoint))
                    return true;

                if (clock != null && clock.TryHandleTap(lcdPoint))
                    return true;
            }

            // On the body: the lever is real geometry, so it is hit-tested in 3D.
            if (deviceShell.LeverHit(pointer.ScreenRay(press.ScreenPosition)))
            {
                FlipMode();
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ states

        void EnterTitle()
        {
            LeaveDemo();
            _flow.EnteredTitle(Time.time);
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            ResetRoundState();
            PresentTitleBarista();
            hud.ShowTitle();
            titleToggles.Render();
            audioService.StartMusic();
        }

        /// <summary>
        /// The LCD look hides Miro so the clock and title stay readable; the painted diorama
        /// keeps him on, wiping the counter off to the side (ScreenStyle.titleBaristaWipes).
        /// </summary>
        void PresentTitleBarista()
        {
            ScreenStyle style = EffectiveStyle;
            bool wipes = style != null && style.titleBaristaWipes;
            if (wipes)
                barista.WipeAt(style.titleBaristaPosition, style.titleBaristaFacesLeft, style.titleWipePeriod);
            barista.SetVisible(wipes);
        }

        /// <summary>The pilot plays a real round with the effects and haptics muted; any press takes over.</summary>
        void EnterDemo()
        {
            audioService.SfxMuted = true;
            _haptics.Muted = true;
            _pilot.Reset();
            _flow.EnteredDemo(Time.time);

            BeginRound();
            hud.ShowDemo();
        }

        void LeaveDemo()
        {
            audioService.SfxMuted = false;
            _haptics.Muted = false;
        }

        /// <summary>
        /// The press that starts the shift is also the first move (OnPressed applies it once
        /// the flow accepts lane moves): tapping the bottom-right quadrant to begin and finding
        /// the barista top-left would cost the first cup.
        /// </summary>
        void StartRound()
        {
            LeaveDemo();
            _flow.RoundStarted();
            BeginRound();
            hud.ShowPlaying();
        }

        void BeginRound()
        {
            ResetRoundState();
            _level = 1;
            hud.SetLevel(1);
            barista.SetVisible(true);
            RenderOrderPanel(); // the flow is already Playing here; ResetRoundState could not show it before
            spawner.BeginRound();
        }

        void ResetRoundState()
        {
            _rolledOver = false;
            ResetShiftEvents();
            _level = 0;
            hud.SetLevel(0);
            _tempo.Reset();
            _score.Reset();
            _penalty.Reset();
            _orders.Reset();
            barista.ResetToDefault();
            deviceShell.ResetAll();
            stainStrip.ResetAll();
            cat.Stop();
            neonFlash.Clear();
            if (neonCat != null)
                neonCat.Stop();
            screenDim.Clear();
            hud.SetScore(_score.DisplayScore);
            RenderOrderPanel();

            foreach (TimedSpriteFx fx in brokenCupFx)
                fx.Hide();
        }

        void OnGameOverTriggered()
        {
            Apply(_flow.PenaltyGameOver());
        }

        void EnterGameOver()
        {
            _flow.EnteredGameOver(Time.time);
            ResetShiftEvents();
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            barista.SetVisible(false); // the result text sits where the barista stands
            orderPanel.Hide();

            int total = _score.TotalScore;
            bool newRecord = _profile.SubmitScore(_mode, total, System.DateTime.Now, out int rank);
            OnlineScores.Backend.Submit(_mode, total); // Play Games; dropped quietly when offline or signed out
            string unlockedName = UnlockSkins(total);

            hud.SetBest(_profile.Best(_mode));
            hud.ShowGameOver(_score.DisplayScore, _profile.Best(_mode), newRecord, unlockedName, rank);

            // Lights down for the night; in the painted diorama Sablé curls up on the counter.
            ScreenStyle style = EffectiveStyle;
            float dim = style != null && style.gameOverDim > 0f ? style.gameOverDim : gameOverDimAlpha;
            screenDim.Hold(dim);
            if (style != null && style.catAsleepA != null)
                cat.Sleep(style.catAsleepA, style.catAsleepB, style.catAsleepX);
            audioService.Play(GameSfx.GameOver);
            _haptics.Pattern(HapticPatterns.Pulses(
                audioConfig.gameOverHapticPulses,
                audioConfig.gameOverHapticMs,
                audioConfig.gameOverHapticGapMs));
        }

        /// <summary>Returns the name of a skin earned this round, or null.</summary>
        string UnlockSkins(int totalScore)
        {
            SkinCatalog.UnlockedBy(_config.unlockSkinId, _config.unlockSkinScore, totalScore, _rolledOver, _unlocks);

            string first = null;
            foreach (string id in _unlocks)
            {
                if (_profile.Unlock(id) && first == null && SkinCatalog.TryGet(id, out Skin skin))
                    first = Loc.Skin(SkinCatalog.IndexOf(id), skin.Name);
            }

            return first;
        }

        // ------------------------------------------------------------------ cups

        /// <summary>Mode B cups are coloured on launch; Mode A leaves the prefab tint alone.</summary>
        void PaintCup(CupController cup)
        {
            if (!_orders.Enabled)
                return;

            // Mode B: the wild machine brews nothing but the order (the terrible ten seconds).
            int colour = _frenzy && cup.Lane == spawner.FrenzyLane ? _orders.CurrentOrder : _orders.RollCupColour();
            cup.Paint(colour, _config.orderColors[colour]);
        }

        /// <summary>
        /// A cup reached K5: the barista's lane at that moment decides the outcome (GDD 2.2),
        /// and in Mode B its colour decides whether it should have been caught at all (GDD 3).
        /// </summary>
        void ResolveCup(CupController cup)
        {
            bool present = (int)barista.Current == cup.Lane && !_climbTarget.HasValue; // mid-climb he is in no slot
            CatchOutcome outcome = CatchRules.Resolve(present, _orders.IsWanted(cup.Colour));

            if (outcome == CatchOutcome.Caught)
            {
                spawner.ReturnCup(cup);
                _score.RegisterCatch();
                _tempo.RegisterCatch();
                _orders.RegisterCorrectCatch();
                barista.ShowCatchPose(_config.catchPoseDuration);
                audioService.Play(GameSfx.Catch);
                _haptics.OneShot(audioConfig.catchHapticMs);
                return;
            }

            if (!CatchRules.IsPenalised(outcome))
            {
                spawner.ReturnCup(cup); // an unwanted colour was correctly let through
                return;
            }

            // Missed or WrongCatch: the cup ends up on the floor either way. The combo breaks
            // now; the crash, the stain and the cat wait until it actually hits the floor.
            _score.RegisterMiss();
            barista.ShowMissPose(_config.missPoseDuration);
            cup.Drop(laneConfig.barLineY, cupFallGravity, cupFallCarry, cupFallSpin);
        }

        /// <summary>A dropped cup reached the floor: it breaks where it landed (review 2026-09-26).</summary>
        void BreakCup(CupController cup)
        {
            Vector2 landing = cup.transform.localPosition;
            spawner.ReturnCup(cup);

            ShowBrokenCup(landing);
            audioService.Play(GameSfx.Miss);
            _haptics.OneShot(audioConfig.missHapticMs);
            cat.Play();
            _penalty.AddStain();
        }

        /// <summary>
        /// The shards stay on the floor until Sablé's mop reaches them (review 2026-09-26: they
        /// used to vanish on their own). With every slot taken the oldest pile is reused.
        /// </summary>
        void ShowBrokenCup(Vector2 position)
        {
            foreach (TimedSpriteFx fx in brokenCupFx)
            {
                if (fx.IsBusy)
                    continue;

                fx.Show(position);
                return;
            }

            if (brokenCupFx.Length > 0)
                brokenCupFx[0].Show(position);
        }

        /// <summary>
        /// The cat walks left to right; a pile vanishes as the mop passes it. A cup that broke
        /// behind a crossing already under way waits for the next one, which starts on its own.
        /// </summary>
        void SweepBrokenCups()
        {
            // Only what the mop passed since the last frame: a pile that lands just behind it stays.
            float mop = cat.IsBusy ? cat.MopX : float.NegativeInfinity;
            float from = mop >= _lastMopX ? _lastMopX : float.NegativeInfinity; // a new crossing restarts on the left
            _lastMopX = mop;

            bool anyLeft = false;
            foreach (TimedSpriteFx fx in brokenCupFx)
            {
                if (!fx.IsBusy)
                    continue;

                float x = fx.Position.x;
                if (cat.IsBusy && x > from && x <= mop)
                    fx.Hide();
                else
                    anyLeft = true;
            }

            if (anyLeft && !cat.IsBusy)
                cat.Play();
        }

        // ------------------------------------------------------------------ events

        void OnOrderChanged(int colour)
        {
            RenderOrderPanel();
        }

        void RenderOrderPanel()
        {
            if (!_orders.Enabled || State == GameState.Title || State == GameState.GameOver)
            {
                orderPanel.Hide();
                return;
            }

            int colour = _orders.CurrentOrder;
            string name = Loc.Order(colour, colour < _config.orderNames.Length ? _config.orderNames[colour] : "");
            orderPanel.Show(colour, _config.orderColors[colour], name);
        }

        void OnCatCrossingStarted()
        {
            if (_catCue.ShouldMeow())
                audioService.Play(GameSfx.CatMeow);
        }

        void OnComboBonusAwarded()
        {
            audioService.Play(GameSfx.ComboBonus);
            neonFlash.Flash();
        }

        void OnRolloverOccurred()
        {
            _rolledOver = true;
            audioService.Play(GameSfx.ComboBonus);
            neonFlash.Flash();
            if (neonCat != null)
                neonCat.Play(_config.rolloverAnimationSeconds); // GDD 2.7: the city neons form the cat
        }

        void OnMercyTriggered()
        {
            if (_penalty.TryRemoveStain())
                cat.Play(force: true);
        }

        void OnBreatherTriggered()
        {
            if (!_flow.TryStartBreather(Time.time, _config.breatherDuration))
                return;

            spawner.SpawningEnabled = false;
            neonFlash.Blink(_config.breatherNeonBlinks, _config.breatherDuration / Mathf.Max(1, _config.breatherNeonBlinks));
            barista.ShowBreather(_config.breatherDuration, _config.breatherWipePeriod);
        }

        void OnSettingsChanged()
        {
            audioService.ApplySettings();
            ApplyScreenStyle();
            if (Loc.Current != _settings.Language)
                Relocalize();
        }

        /// <summary>English / Polish from the menu: every line on screen is set again.</summary>
        void Relocalize()
        {
            Loc.Set(_settings.Language);
            hud.Relocalize();
            titleToggles.Render();
            RenderOrderPanel();
            if (menu.IsOpen)
                menu.Render(_settings, _mode);
        }

        void OnProfileChanged()
        {
            ApplySkin();
        }

        void ApplySkin()
        {
            SkinCatalog.TryGet(_profile.SelectedSkin, out Skin skin);
            deviceShell.ApplySkin(skin);
        }
    }
}
