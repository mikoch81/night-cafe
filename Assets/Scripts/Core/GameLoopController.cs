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
    /// Title -> Playing -> Breather -> GameOver state machine.
    /// This is the only place that decides what a catch or a miss means.
    /// </summary>
    public sealed class GameLoopController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] ModeConfig modeConfig;
        [SerializeField] LaneConfig laneConfig;
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
        [SerializeField] FlashFx neonFlash;
        [SerializeField] FlashFx screenDim;
        [SerializeField] TitleToggleView titleToggles;
        [SerializeField] AudioService audioService;
        [SerializeField] Camera worldCamera;

        [Header("Platform")]
        [SerializeField] int targetFrameRate = 60;
        [SerializeField] float gameOverDimAlpha = 0.55f;

        TempoService _tempo;
        ScoreService _score;
        PenaltyService _penalty;
        SettingsService _settings;
        HapticsService _haptics;
        CatCueService _catCue;
        float _breatherEndsAt;
        bool _tapConsumed;

        public GameState State { get; private set; } = GameState.Title;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            VerifyFxSharesCupSpace();

            _tempo = new TempoService(modeConfig.Tempo);
            _score = new ScoreService(modeConfig.Score);
            _penalty = new PenaltyService(modeConfig.maxStains);
            _settings = new SettingsService(new PlayerPrefsSettingsStore());
            _haptics = new HapticsService(HapticsService.CreateBackend(), _settings);
            _catCue = new CatCueService(audioConfig.meowChance, new UnityRandom());

            _score.ScoreChanged += hud.SetScore;
            _score.MercyTriggered += OnMercyTriggered;
            _score.BreatherTriggered += OnBreatherTriggered;
            _score.ComboBonusAwarded += OnComboBonusAwarded;
            _score.RolloverOccurred += OnRolloverOccurred;
            _penalty.StainsChanged += stainStrip.SetStains;
            _penalty.GameOverTriggered += EnterGameOver;
            _settings.Changed += OnSettingsChanged;

            spawner.Initialise(modeConfig, laneConfig, _tempo, new UnityRandom());
            spawner.CupReachedCatchPoint += ResolveCup;

            barista.Initialise(laneConfig);

            audioService.Initialise(_settings);
            titleToggles.Initialise(_settings, worldCamera);
            cat.CrossingStarted += OnCatCrossingStarted;

            laneInput.PositionPressed += OnPositionPressed;
            laneInput.Tapped += OnTapped;
            laneInput.AnyPressed += OnAnyPressed;
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
            if (spawner != null)
                spawner.CupReachedCatchPoint -= ResolveCup;

            if (cat != null)
                cat.CrossingStarted -= OnCatCrossingStarted;

            if (laneInput != null)
            {
                laneInput.PositionPressed -= OnPositionPressed;
                laneInput.Tapped -= OnTapped;
                laneInput.AnyPressed -= OnAnyPressed;
            }
        }

        void Update()
        {
            if (State != GameState.Breather)
                return;

            if (Time.time >= _breatherEndsAt)
            {
                State = GameState.Playing;
                spawner.ResumeAfterBreather();
            }
        }

        void OnPositionPressed(LanePosition position)
        {
            deviceShell.Press(position);

            if (State is GameState.Playing or GameState.Breather)
                barista.MoveTo(position);
        }

        /// <summary>Consumed by the title toggles before it can start a round.</summary>
        void OnTapped(Vector2 screenPosition)
        {
            if (State != GameState.Title || screenPosition.x < 0f)
                return;

            _tapConsumed = titleToggles.TryHandleTap(screenPosition);
        }

        void OnAnyPressed()
        {
            if (_tapConsumed)
            {
                _tapConsumed = false;
                return;
            }

            if (State is GameState.Title or GameState.GameOver)
                StartRound();
        }

        void EnterTitle()
        {
            State = GameState.Title;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            ResetRoundState();
            barista.SetVisible(false);
            hud.ShowTitle();
            titleToggles.Render();
            audioService.StartMusic();
        }

        void StartRound()
        {
            ResetRoundState();
            barista.SetVisible(true);
            State = GameState.Playing;
            hud.ShowPlaying();
            spawner.BeginRound();
        }

        void ResetRoundState()
        {
            _tempo.Reset();
            _score.Reset();
            _penalty.Reset();
            barista.ResetToDefault();
            deviceShell.ResetAll();
            stainStrip.ResetAll();
            cat.Stop();
            neonFlash.Clear();
            screenDim.Clear();
            hud.SetScore(_score.DisplayScore);

            foreach (TimedSpriteFx fx in brokenCupFx)
                fx.Hide();
        }

        /// <summary>
        /// A cup reached K5: the barista's lane at that moment decides the outcome (GDD 2.2).
        /// </summary>
        void ResolveCup(CupController cup)
        {
            bool caught = (int)barista.Current == cup.Lane;
            Vector2 landing = cup.transform.localPosition;

            spawner.ReturnCup(cup);

            if (caught)
            {
                _score.RegisterCatch();
                _tempo.RegisterCatch();
                barista.ShowCatchPose(modeConfig.catchPoseDuration);
                audioService.Play(GameSfx.Catch);
                _haptics.OneShot(audioConfig.catchHapticMs);
                return;
            }

            _score.RegisterMiss();
            barista.ShowMissPose(modeConfig.missPoseDuration);
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

                fx.Show(position, modeConfig.brokenCupDuration);
                return;
            }
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
            _breatherEndsAt = Time.time + modeConfig.breatherDuration;
            neonFlash.Flash();
        }

        void OnSettingsChanged()
        {
            audioService.ApplySettings();
        }

        void EnterGameOver()
        {
            State = GameState.GameOver;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            barista.SetVisible(false); // the result text sits where the barista stands
            hud.ShowGameOver(_score.DisplayScore);
            screenDim.Hold(gameOverDimAlpha);
            audioService.Play(GameSfx.GameOver);
            _haptics.Pattern(HapticPatterns.Pulses(
                audioConfig.gameOverHapticPulses,
                audioConfig.gameOverHapticMs,
                audioConfig.gameOverHapticGapMs));
        }
    }
}
