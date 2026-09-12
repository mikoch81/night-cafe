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
        [SerializeField] GameObject ghostRoot;
        [SerializeField] FlashFx neonFlash;
        [SerializeField] FlashFx screenDim;
        [SerializeField] TitleToggleView titleToggles;
        [SerializeField] AudioService audioService;
        [SerializeField] Camera worldCamera;

        [Header("Platform")]
        [SerializeField] int targetFrameRate = 60;
        [SerializeField] float gameOverDimAlpha = 0.55f;

        readonly List<PilotCup> _pilotCups = new(8);
        readonly List<string> _unlocks = new(2);

        // Mode-independent
        SettingsService _settings;
        ProfileService _profile;
        HapticsService _haptics;
        CatCueService _catCue;
        AttractPilot _pilot;

        // Rebuilt whenever the lever flips
        GameMode _mode;
        ModeConfig _config;
        TempoService _tempo;
        ScoreService _score;
        PenaltyService _penalty;
        OrderService _orders;

        float _breatherEndsAt;
        float _idleSince;
        float _demoEndsAt;
        bool _tapConsumed;
        bool _rolledOver;

        public GameState State { get; private set; } = GameState.Title;

        /// <summary>True while the attract pilot is playing on the title screen (GDD 6).</summary>
        public bool IsDemo { get; private set; }

        public GameMode Mode => _mode;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            VerifyFxSharesCupSpace();

            _settings = new SettingsService(new PlayerPrefsSettingsStore());
            _profile = new ProfileService(FileProfileStore.Default());
            _haptics = new HapticsService(HapticsService.CreateBackend(), _settings);
            _catCue = new CatCueService(audioConfig.meowChance, new UnityRandom());
            _pilot = new AttractPilot(new UnityRandom(), deviceConfig.pilotReactionSeconds,
                deviceConfig.pilotFumbleChance, LaneConfig.StepsPerLane - 1);

            _settings.Changed += OnSettingsChanged;
            _profile.Changed += OnProfileChanged;

            spawner.CupReachedCatchPoint += ResolveCup;
            spawner.CupSpawned += PaintCup;

            barista.Initialise(laneConfig);
            audioService.Initialise(_settings);
            titleToggles.Initialise(_settings, _profile, worldCamera);
            cat.CrossingStarted += OnCatCrossingStarted;

            laneInput.PositionPressed += OnPositionPressed;
            laneInput.Tapped += OnTapped;
            laneInput.AnyPressed += OnAnyPressed;

            ConfigureMode(_profile.SelectedMode);
            ApplySkin();
            ApplyGhosts();
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
                spawner.CupSpawned -= PaintCup;
            }

            if (cat != null)
                cat.CrossingStarted -= OnCatCrossingStarted;

            if (laneInput != null)
            {
                laneInput.PositionPressed -= OnPositionPressed;
                laneInput.Tapped -= OnTapped;
                laneInput.AnyPressed -= OnAnyPressed;
            }
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
            hud.SetBest(_profile.Best(mode), _config.rolloverModulo);
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
            audioService.Play(GameSfx.Catch);
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            float dt = Time.deltaTime;

            switch (State)
            {
                case GameState.Title:
                    if (Time.time - _idleSince >= deviceConfig.attractDelay)
                        EnterDemo();
                    return;

                case GameState.Breather:
                    _orders.Tick(dt);
                    if (Time.time >= _breatherEndsAt)
                    {
                        State = GameState.Playing;
                        spawner.ResumeAfterBreather();
                    }
                    break;

                case GameState.Playing:
                    _orders.Tick(dt);
                    break;

                default:
                    return;
            }

            if (IsDemo)
                UpdateDemo(dt);
        }

        void UpdateDemo(float dt)
        {
            if (Time.time >= _demoEndsAt)
            {
                EnterTitle();
                return;
            }

            _pilotCups.Clear();
            IReadOnlyList<CupController> cups = spawner.ActiveCups;
            for (int i = 0; i < cups.Count; i++)
                _pilotCups.Add(new PilotCup(cups[i].Serial, cups[i].Lane, cups[i].StepIndex, _orders.IsWanted(cups[i].Colour)));

            int lane = _pilot.Decide(_pilotCups, (int)barista.Current, dt);
            if (lane < 0)
                return;

            var position = (LanePosition)lane;
            deviceShell.Press(position);
            barista.MoveTo(position);
        }

        // ------------------------------------------------------------------ input

        void OnPositionPressed(LanePosition position)
        {
            deviceShell.Press(position);

            if ((State is GameState.Playing or GameState.Breather) && !IsDemo)
                barista.MoveTo(position);
        }

        /// <summary>Title-screen hit testing: toggles and the lever consume the tap before it can start a round.</summary>
        void OnTapped(Vector2 screenPosition)
        {
            if (State != GameState.Title)
                return;

            _idleSince = Time.time;

            if (screenPosition.x < 0f)
                return;

            if (titleToggles.TryHandleTap(screenPosition))
            {
                _tapConsumed = true;
                return;
            }

            if (deviceShell.LeverHit(worldCamera.ScreenToWorldPoint(screenPosition)))
            {
                FlipMode();
                _tapConsumed = true;
            }
        }

        void OnAnyPressed()
        {
            if (_tapConsumed)
            {
                _tapConsumed = false;
                return;
            }

            if (State is GameState.Title or GameState.GameOver || IsDemo)
                StartRound();
        }

        // ------------------------------------------------------------------ states

        void EnterTitle()
        {
            LeaveDemo();
            State = GameState.Title;
            _idleSince = Time.time;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            ResetRoundState();
            barista.SetVisible(false);
            hud.ShowTitle();
            titleToggles.Render();
            audioService.StartMusic();
        }

        /// <summary>The pilot plays a real round with the effects and haptics muted; any press takes over.</summary>
        void EnterDemo()
        {
            IsDemo = true;
            audioService.SfxMuted = true;
            _haptics.Muted = true;
            _pilot.Reset();
            _demoEndsAt = Time.time + deviceConfig.attractMaxDuration;

            BeginRound();
            hud.ShowDemo();
        }

        void LeaveDemo()
        {
            IsDemo = false;
            audioService.SfxMuted = false;
            _haptics.Muted = false;
        }

        void StartRound()
        {
            LeaveDemo();
            BeginRound();
            hud.ShowPlaying();
        }

        void BeginRound()
        {
            ResetRoundState();
            barista.SetVisible(true);
            State = GameState.Playing;
            RenderOrderPanel(); // ResetRoundState hid it while the state was still Title / GameOver
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
            screenDim.Clear();
            hud.SetScore(_score.DisplayScore);
            RenderOrderPanel();

            foreach (TimedSpriteFx fx in brokenCupFx)
                fx.Hide();
        }

        void OnGameOverTriggered()
        {
            // A demo that runs out of stains just hands the title screen back.
            if (IsDemo)
                EnterTitle();
            else
                EnterGameOver();
        }

        void EnterGameOver()
        {
            State = GameState.GameOver;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            barista.SetVisible(false); // the result text sits where the barista stands
            orderPanel.Hide();

            int total = _score.TotalScore;
            bool newRecord = _profile.SubmitScore(_mode, total);
            string unlockedName = UnlockSkins(total);

            hud.SetBest(_profile.Best(_mode), _config.rolloverModulo);
            hud.ShowGameOver(_score.DisplayScore, _profile.Best(_mode) % _config.rolloverModulo, newRecord, unlockedName);
            screenDim.Hold(gameOverDimAlpha);
            audioService.Play(GameSfx.GameOver);
            _haptics.Pattern(HapticPatterns.Pulses(
                audioConfig.gameOverHapticPulses,
                audioConfig.gameOverHapticMs,
                audioConfig.gameOverHapticGapMs));
        }

        /// <summary>Returns the name of a skin earned this round, or null.</summary>
        string UnlockSkins(int totalScore)
        {
            SkinCatalog.UnlockedBy(_mode, totalScore, _rolledOver, _unlocks);

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
            Vector2 landing = cup.transform.localPosition;

            spawner.ReturnCup(cup);

            switch (outcome)
            {
                case CatchOutcome.Caught:
                    _score.RegisterCatch();
                    _tempo.RegisterCatch();
                    _orders.RegisterCorrectCatch();
                    barista.ShowCatchPose(_config.catchPoseDuration);
                    audioService.Play(GameSfx.Catch);
                    _haptics.OneShot(audioConfig.catchHapticMs);
                    return;

                case CatchOutcome.Ignored:
                    return;
            }

            // Missed or WrongCatch: the cup ends up on the floor either way.
            _score.RegisterMiss();
            barista.ShowMissPose(_config.missPoseDuration);
            ShowBrokenCup(landing);
            audioService.Play(GameSfx.Miss);
            _haptics.OneShot(audioConfig.missHapticMs);
            cat.Play();
            _penalty.AddStain();
        }

        void ShowBrokenCup(Vector2 position)
        {
            foreach (TimedSpriteFx fx in brokenCupFx)
            {
                if (fx.IsBusy)
                    continue;

                fx.Show(position, _config.brokenCupDuration);
                return;
            }
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
            orderPanel.Show(_config.orderColors[colour], name);
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
            // GDD 2.7's neon-cat animation is a later milestone; the bar still marks the moment.
            _rolledOver = true;
            audioService.Play(GameSfx.ComboBonus);
            neonFlash.Flash();
        }

        void OnMercyTriggered()
        {
            if (_penalty.TryRemoveStain())
                cat.Play(force: true);
        }

        void OnBreatherTriggered()
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Breather;
            spawner.SpawningEnabled = false;
            _breatherEndsAt = Time.time + _config.breatherDuration;
            neonFlash.Flash();
        }

        void OnSettingsChanged()
        {
            audioService.ApplySettings();
            ApplyGhosts();
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

        void ApplyGhosts()
        {
            if (ghostRoot != null)
                ghostRoot.SetActive(_settings.GhostsEnabled);
        }
    }
}
