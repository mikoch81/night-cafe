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

        [Header("Scene references")]
        [SerializeField] LaneSpawner spawner;
        [SerializeField] LaneInput laneInput;
        [SerializeField] PlayerPositionController barista;
        [SerializeField] HudView hud;
        [SerializeField] TimedSpriteFx[] brokenCupFx;

        [Header("Platform")]
        [SerializeField] int targetFrameRate = 60;

        TempoService _tempo;
        ScoreService _score;
        PenaltyService _penalty;
        float _breatherEndsAt;

        public GameState State { get; private set; } = GameState.Title;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            _tempo = new TempoService(modeConfig.Tempo);
            _score = new ScoreService(modeConfig.Score);
            _penalty = new PenaltyService(modeConfig.maxStains);

            _score.ScoreChanged += hud.SetScore;
            _score.MercyTriggered += OnMercyTriggered;
            _score.BreatherTriggered += OnBreatherTriggered;
            _penalty.StainsChanged += hud.SetStains;
            _penalty.GameOverTriggered += EnterGameOver;

            spawner.Initialise(modeConfig, laneConfig, _tempo, new UnityRandom());
            spawner.CupReachedCatchPoint += ResolveCup;

            barista.Initialise(laneConfig);

            laneInput.PositionPressed += OnPositionPressed;
            laneInput.AnyPressed += OnAnyPressed;
        }

        void Start()
        {
            EnterTitle();
        }

        void OnDestroy()
        {
            if (spawner != null)
                spawner.CupReachedCatchPoint -= ResolveCup;

            if (laneInput != null)
            {
                laneInput.PositionPressed -= OnPositionPressed;
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
            if (State is GameState.Playing or GameState.Breather)
                barista.MoveTo(position);
        }

        void OnAnyPressed()
        {
            if (State is GameState.Title or GameState.GameOver)
                StartRound();
        }

        void EnterTitle()
        {
            State = GameState.Title;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            ResetRoundState();
            hud.ShowTitle();
        }

        void StartRound()
        {
            ResetRoundState();
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
            hud.SetScore(_score.DisplayScore);
            hud.SetStains(_penalty.Stains);

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
                return;
            }

            _score.RegisterMiss();
            barista.ShowMissPose(modeConfig.missPoseDuration);
            ShowBrokenCup(landing);
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

        void OnMercyTriggered()
        {
            _penalty.TryRemoveStain();
        }

        void OnBreatherTriggered()
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Breather;
            spawner.SpawningEnabled = false;
            _breatherEndsAt = Time.time + modeConfig.breatherDuration;
        }

        void EnterGameOver()
        {
            State = GameState.GameOver;
            spawner.SpawningEnabled = false;
            spawner.DespawnAll();
            hud.ShowGameOver(_score.DisplayScore);
        }
    }
}
