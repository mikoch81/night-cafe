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
        [SerializeField] GameObject demoPanel;
        [SerializeField] TMP_Text demoText;
        [SerializeField] float demoBlinkSeconds = 0.6f;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] TMP_Text gameOverHeader;
        [SerializeField] TMP_Text gameOverScore;
        [SerializeField] TMP_Text gameOverRecord;
        [SerializeField] TMP_Text gameOverFooter;
        [SerializeField] TMP_Text fpsText;

        Color _recordColor = Color.white;
        Color _accentColor = Color.white;
        bool _colorsSet;

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
        }

        public void SetScore(int displayScore)
        {
            if (scoreText != null)
                scoreText.text = displayScore.ToString("000");
        }

        /// <summary>The record wears the same three-digit counter, so it is shown modulo the rollover.</summary>
        public void SetBest(int bestTotal, int rolloverModulo)
        {
            if (bestText != null)
                bestText.text = $"BEST {bestTotal % rolloverModulo:000}";
        }

        public void SetMode(GameMode mode)
        {
            if (modeText != null)
                modeText.text = mode.Letter();
        }

        public void ShowTitle()
        {
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

        public void ShowGameOver(int displayScore, int bestDisplay, bool newRecord, string unlockedSkin)
        {
            SetPanel(titlePanel, false);
            SetPanel(demoPanel, false);
            SetPanel(gameOverPanel, true);

            GameOverCopy copy = GameOverCopy.Build(displayScore, bestDisplay, newRecord, unlockedSkin);
            SetText(gameOverHeader, copy.Header);
            SetText(gameOverScore, copy.Score);
            SetText(gameOverRecord, copy.Record);
            SetText(gameOverFooter, copy.Footer);
            if (gameOverRecord != null)
                gameOverRecord.color = copy.NewRecord ? _accentColor : _recordColor;
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
