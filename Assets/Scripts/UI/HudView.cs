using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The counter and the title / game over overlays (GDD 5.1 layer HUD_TMP).
    /// These are world-space TextMeshPro objects nested inside the LCD, not a screen overlay:
    /// an overlay canvas renders outside the sorting-layer system and would sit on the wood.
    /// The FPS readout is the exception - it is debug output and stays on a screen canvas.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] TMP_Text gameOverText;
        [SerializeField] TMP_Text fpsText;

        float _fpsAccumulator;
        int _fpsFrames;

        void Awake()
        {
            if (fpsText != null)
                fpsText.gameObject.SetActive(Debug.isDebugBuild);
        }

        public void SetScore(int displayScore)
        {
            if (scoreText != null)
                scoreText.text = displayScore.ToString("000");
        }

        public void ShowTitle()
        {
            SetPanel(titlePanel, true);
            SetPanel(gameOverPanel, false);
        }

        public void ShowPlaying()
        {
            SetPanel(titlePanel, false);
            SetPanel(gameOverPanel, false);
        }

        public void ShowGameOver(int displayScore)
        {
            SetPanel(titlePanel, false);
            SetPanel(gameOverPanel, true);

            if (gameOverText != null)
                gameOverText.text = $"END OF SHIFT\n{displayScore:000}\nTAP TO RESTART";
        }

        void Update()
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
