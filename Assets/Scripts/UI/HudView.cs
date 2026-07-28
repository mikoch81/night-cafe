using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NightCafe.UI
{
    /// <summary>
    /// Score counter, stain icons and the Title / Game Over overlays (GDD 5.1 layer HUD_TMP).
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        static readonly Color ActiveColor = new(1f, 0.788f, 0.4f);      // #ffc966
        static readonly Color InactiveColor = new(0.227f, 0.173f, 0.094f); // #3a2c18

        [SerializeField] TMP_Text scoreText;
        [SerializeField] Image[] stainIcons;
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

        public void SetStains(int stains)
        {
            if (stainIcons == null)
                return;

            for (int i = 0; i < stainIcons.Length; i++)
            {
                if (stainIcons[i] != null)
                    stainIcons[i].color = i < stains ? ActiveColor : InactiveColor;
            }
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
