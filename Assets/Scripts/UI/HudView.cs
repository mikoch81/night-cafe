using NightCafe.Core;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The counter, the best score, the mode letter and the title / demo / game over overlays
    /// (GDD 5.1 layer HUD_TMP). These are world-space TextMeshPro objects nested inside the LCD,
    /// not a screen overlay: an overlay canvas renders outside the sorting-layer system and would
    /// sit on the wood. The FPS readout is the exception - it is debug output and stays on a
    /// screen canvas.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text bestText;
        [SerializeField] TMP_Text modeText;
        [SerializeField] GameObject titlePanel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] GameObject demoPanel;
        [SerializeField] TMP_Text demoText;
        [SerializeField] float demoBlinkSeconds = 0.6f;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] TMP_Text gameOverHeader;
        [SerializeField] TMP_Text gameOverScore;
        [SerializeField] TMP_Text gameOverRecord;
        [SerializeField] TMP_Text gameOverFooter;
        [SerializeField] TMP_Text fpsText;

        [Header("Levels and café events (1.1.0)")]
        [SerializeField] TMP_Text levelText;
        [Tooltip("A strip of dark glass with one line on it: LEVEL n for a moment, RUSH HOUR while it lasts.")]
        [SerializeField] GameObject bannerRoot;
        [SerializeField] TMP_Text bannerText;

        string _flash;
        float _flashUntil;
        int _countdown = -1;
        Txt _countdownLabel = Txt.RushHour;
        int _best;
        int _level;

        Color _recordColor = Color.white;
        Color _accentColor = Color.white;
        bool _colorsSet;

        // 1.2.0 juice: catch popups, a bump on the counter, the banner unrolling, the result counting up.
        const int PopCount = 4;
        const float PopSeconds = 0.6f;
        const float PopRise = 0.7f;
        const float PunchSeconds = 0.12f;
        const float BannerSeconds = 0.15f;
        const float CountUpSeconds = 0.8f;
        readonly TMP_Text[] _pops = new TMP_Text[PopCount];
        readonly float[] _popLeft = new float[PopCount];
        readonly Vector3[] _popFrom = new Vector3[PopCount];
        int _nextPop;
        int _lastScore;
        float _punchLeft;
        Vector3 _scoreScale = Vector3.one;
        float _bannerOpen;     // 0 rolled up .. 1 open
        bool _bannerWanted;
        int _countTarget;
        float _countTime = -1f;
        bool _recordPulse;
        Vector3 _recordScale = Vector3.one;

        float _fpsAccumulator;
        int _fpsFrames;
        float _blinkTimer;

        void Awake()
        {
            if (fpsText != null)
                fpsText.gameObject.SetActive(Debug.isDebugBuild);

            // Until a style says otherwise the record line keeps the colour it was authored in.
            if (!_colorsSet && gameOverRecord != null)
                _recordColor = _accentColor = gameOverRecord.color;
            if (scoreText != null)
                _scoreScale = scoreText.transform.localScale;
            if (gameOverRecord != null)
                _recordScale = gameOverRecord.transform.localScale;
        }

        public void SetScore(int displayScore)
        {
            if (scoreText != null)
                scoreText.text = displayScore.ToString("000");
            if (displayScore > _lastScore)
                _punchLeft = PunchSeconds;
            _lastScore = displayScore;
        }

        /// <summary>
        /// "+1" (or "+6  x25") rising from `world` (a point in the LCD scene) and fading. The
        /// popups are clones of the banner line, made on first use so they carry the screen style.
        /// </summary>
        public void Pop(Vector3 world, string text, bool big)
        {
            if (bannerText == null)
                return;

            int i = _nextPop;
            _nextPop = (i + 1) % PopCount;
            if (_pops[i] == null)
            {
                _pops[i] = Instantiate(bannerText, transform);
                _pops[i].name = "Pop";
                _pops[i].textWrappingMode = TextWrappingModes.NoWrap;
            }

            TMP_Text pop = _pops[i];
            pop.text = text;
            pop.color = scoreText != null ? scoreText.color : bannerText.color;
            pop.transform.localScale = Vector3.one * (big ? 1.35f : 1f);
            world.z = bannerText.transform.position.z;
            _popFrom[i] = transform.InverseTransformPoint(world);
            pop.transform.localPosition = _popFrom[i];
            _popLeft[i] = PopSeconds;
            pop.gameObject.SetActive(true);
        }

        void UpdatePops(float dt)
        {
            for (int i = 0; i < PopCount; i++)
            {
                if (_pops[i] == null || _popLeft[i] <= 0f)
                    continue;

                _popLeft[i] -= dt;
                float t = 1f - Mathf.Clamp01(_popLeft[i] / PopSeconds);
                _pops[i].transform.localPosition = _popFrom[i] + Vector3.up * (PopRise * (1f - (1f - t) * (1f - t)));
                Color c = _pops[i].color;
                c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
                _pops[i].color = c;
                if (_popLeft[i] <= 0f)
                    _pops[i].gameObject.SetActive(false);
            }
        }

        void HidePops()
        {
            for (int i = 0; i < PopCount; i++)
            {
                _popLeft[i] = 0f;
                if (_pops[i] != null)
                    _pops[i].gameObject.SetActive(false);
            }
        }

        void UpdatePunch(float dt)
        {
            if (scoreText == null || _punchLeft <= 0f)
                return;

            _punchLeft -= dt;
            float t = 1f - Mathf.Clamp01(_punchLeft / PunchSeconds);
            scoreText.transform.localScale = _scoreScale * (1f + 0.12f * Mathf.Sin(t * Mathf.PI));
        }

        /// <summary>The whole record, three digits at least, like the counter.</summary>
        public void SetBest(int bestTotal)
        {
            _best = bestTotal;
            if (bestText != null)
                bestText.text = Loc.F(Txt.HudBest, bestTotal);
        }

        /// <summary>LV n beside the mode letter; hidden (0) off shift.</summary>
        public void SetLevel(int level)
        {
            _level = level;
            if (levelText == null)
                return;

            levelText.gameObject.SetActive(level > 0);
            levelText.text = Loc.F(Txt.HudLevel, level);
        }

        /// <summary>The language changed: every line the HUD owns is set again.</summary>
        public void Relocalize()
        {
            SetBest(_best);
            SetLevel(_level);
            SetText(titleText, Loc.T(Txt.TitleTapToStart));
            SetText(demoText, Loc.T(Txt.DemoTapToStart));
            RenderBanner();
        }

        /// <summary>A line on the banner for a few seconds (LEVEL 3); it wins over the rush countdown.</summary>
        public void FlashBanner(string text, float seconds)
        {
            _flash = text;
            _flashUntil = Time.time + seconds;
            RenderBanner();
        }

        /// <summary>RUSH HOUR with the seconds left, or -1 to take it down.</summary>
        public void SetRushCountdown(int seconds) => SetCountdown(Txt.RushHour, seconds);

        /// <summary>A running event's name and the seconds left (RUSH HOUR 42, TERRIBLE TEN 7); -1 takes it down.</summary>
        public void SetCountdown(Txt label, int seconds)
        {
            if (seconds == _countdown && label == _countdownLabel)
                return;

            _countdown = seconds;
            _countdownLabel = label;
            RenderBanner();
        }

        public void ClearBanner()
        {
            _flash = null;
            _countdown = -1;
            RenderBanner();
        }

        void RenderBanner()
        {
            if (bannerRoot == null || bannerText == null)
                return;

            string line = _flash != null && Time.time < _flashUntil ? _flash
                : _countdown >= 0 ? $"{Loc.T(_countdownLabel)}  {_countdown}"
                : null;
            _bannerWanted = line != null;
            if (line == null)
                return; // rolls up in UpdateBanner, still showing its last line

            bannerText.text = line;
            if (!bannerRoot.activeSelf)
            {
                _bannerOpen = 0f;
                bannerRoot.transform.localScale = new Vector3(0f, 1f, 1f);
                bannerRoot.SetActive(true);
            }
        }

        /// <summary>The strip unrolls from the middle and rolls back up (its children sit at x = 0).</summary>
        void UpdateBanner(float dt)
        {
            if (bannerRoot == null || !bannerRoot.activeSelf)
                return;

            float target = _bannerWanted ? 1f : 0f;
            if (Mathf.Approximately(_bannerOpen, target))
                return;

            _bannerOpen = Mathf.MoveTowards(_bannerOpen, target, dt / BannerSeconds);
            float eased = 1f - (1f - _bannerOpen) * (1f - _bannerOpen);
            bannerRoot.transform.localScale = new Vector3(eased, 1f, 1f);
            if (_bannerOpen <= 0f)
                bannerRoot.SetActive(false);
        }

        public void SetMode(GameMode mode)
        {
            if (modeText != null)
                modeText.text = mode.Letter();
        }

        public void ShowTitle()
        {
            HidePops();
            SetPanel(titlePanel, true);
            SetPanel(demoPanel, false);
            SetPanel(gameOverPanel, false);
        }

        public void ShowDemo()
        {
            SetPanel(titlePanel, false);
            SetPanel(demoPanel, true);
            SetPanel(gameOverPanel, false);
            _blinkTimer = 0f;
        }

        public void ShowPlaying()
        {
            SetPanel(titlePanel, false);
            SetPanel(demoPanel, false);
            SetPanel(gameOverPanel, false);
        }

        public void ShowGameOver(int displayScore, int bestDisplay, bool newRecord, string unlockedSkin, int rank)
        {
            SetPanel(titlePanel, false);
            SetPanel(demoPanel, false);
            SetPanel(gameOverPanel, true);
            HidePops();

            GameOverCopy copy = GameOverCopy.Build(displayScore, bestDisplay, newRecord, unlockedSkin, rank);
            SetText(gameOverHeader, copy.Header);
            SetText(gameOverScore, copy.Score);
            SetText(gameOverRecord, copy.Record);
            SetText(gameOverFooter, copy.Footer);
            if (gameOverRecord != null)
                gameOverRecord.color = copy.NewRecord ? _accentColor : _recordColor;

            // The result counts up from zero; a new best keeps beating.
            _countTarget = displayScore;
            _countTime = 0f;
            _recordPulse = copy.NewRecord;
            if (gameOverRecord != null)
                gameOverRecord.transform.localScale = _recordScale;
            if (gameOverScore != null)
                gameOverScore.text = 0.ToString("000");
        }

        void UpdateGameOverCard(float dt)
        {
            if (gameOverPanel == null || !gameOverPanel.activeSelf)
                return;

            if (_countTime >= 0f && gameOverScore != null)
            {
                _countTime += dt;
                float t = Mathf.Clamp01(_countTime / CountUpSeconds);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                gameOverScore.text = Mathf.RoundToInt(_countTarget * eased).ToString("000");
                if (t >= 1f)
                    _countTime = -1f;
            }

            if (_recordPulse && gameOverRecord != null)
                gameOverRecord.transform.localScale = _recordScale * (1f + 0.06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.5f)));
        }

        /// <summary>
        /// Screen style: the record line's plain and celebratory colours (ink on the painted
        /// receipt, amber on the LCD). Applied on the next ShowGameOver.
        /// </summary>
        public void SetRecordColors(Color plain, Color accent)
        {
            _recordColor = plain;
            _accentColor = accent;
            _colorsSet = true;
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        void Update()
        {
            if (_flash != null && Time.time >= _flashUntil)
            {
                _flash = null;
                RenderBanner();
            }

            float dt = Time.deltaTime;
            UpdatePops(dt);
            UpdatePunch(dt);
            UpdateBanner(Time.unscaledDeltaTime);
            UpdateGameOverCard(Time.unscaledDeltaTime);
            UpdateDemoBlink();
            UpdateFps();
        }

        void UpdateDemoBlink()
        {
            if (demoText == null || demoPanel == null || !demoPanel.activeSelf)
                return;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer < demoBlinkSeconds)
                return;

            _blinkTimer = 0f;
            demoText.enabled = !demoText.enabled;
        }

        void UpdateFps()
        {
            if (fpsText == null || !fpsText.gameObject.activeSelf)
                return;

            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrames++;

            if (_fpsAccumulator < 0.5f)
                return;

            fpsText.text = $"{_fpsFrames / _fpsAccumulator:0} fps";
            _fpsAccumulator = 0f;
            _fpsFrames = 0;
        }

        static void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }
    }
}
