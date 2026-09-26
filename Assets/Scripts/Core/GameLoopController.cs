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

        public GameState State => _flow.State;

        /// <summary>True while the attract pilot is playing on the title screen (GDD 6).</summary>
        public bool IsDemo => _flow.IsDemo;

        public GameMode Mode => _mode;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            VerifyFxSharesCupSpace();

            _settings = new SettingsService(new PlayerPrefsSettingsStore());
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

            laneInput.Pressed += OnPressed;

            ConfigureMode(_profile.SelectedMode);
            ApplySkin();
            ApplyScreenStyle();
        }

        /// <summary>GDD 5.2: the painted diorama, the one screen style since RETRO was dropped.</summary>
        public ScreenStyle EffectiveStyle => artStyle;

        void ApplyScreenStyle()
        {
            if (styleApplier != null)
                styleApplier.Apply(EffectiveStyle);
            if (audioService != null)
                audioService.SetSoundSet(EffectiveStyle != null ? EffectiveStyle.sounds : null);

            // The applier parks Miro back on his lane slot; on the title he belongs at the counter.
            if (_flow != null && State == GameState.Title)
                PresentTitleBarista();
        }

        void Start()
        {
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

            _score.ScoreChanged += hud.SetScore;
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
                barista.MoveTo(press.Lane.Value);
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
            menu.Open(_menuPausedRound, _settings, _mode);
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
                _profile.SubmitScore(_mode, _score.TotalScore, System.DateTime.Now, out _);

            FlipMode();
            if (State != GameState.Title)
                EnterTitle();

            menu.Open(false, _settings, _mode);
            _menuPausedRound = false;
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
            barista.SetVisible(true);
            RenderOrderPanel(); // the flow is already Playing here; ResetRoundState could not show it before
            spawner.BeginRound();
        }

        void ResetRoundState()
        {
            _rolledOver = false;
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
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            barista.SetVisible(false); // the result text sits where the barista stands
            orderPanel.Hide();

            int total = _score.TotalScore;
            bool newRecord = _profile.SubmitScore(_mode, total, System.DateTime.Now, out int rank);
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
                    first = skin.Name;
            }

            return first;
        }

        // ------------------------------------------------------------------ cups

        /// <summary>Mode B cups are coloured on launch; Mode A leaves the prefab tint alone.</summary>
        void PaintCup(CupController cup)
        {
            if (!_orders.Enabled)
                return;

            int colour = _orders.RollCupColour();
            cup.Paint(colour, _config.orderColors[colour]);
        }

        /// <summary>
        /// A cup reached K5: the barista's lane at that moment decides the outcome (GDD 2.2),
        /// and in Mode B its colour decides whether it should have been caught at all (GDD 3).
        /// </summary>
        void ResolveCup(CupController cup)
        {
            bool present = (int)barista.Current == cup.Lane;
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
            string name = colour < _config.orderNames.Length ? _config.orderNames[colour] : "";
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
