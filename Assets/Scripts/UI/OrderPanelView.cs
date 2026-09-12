using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// Mode B order light above the bar (GDD 3). order_panel.png is an amber frame with a
    /// white cup; the cup is coloured by a quad laid over it rather than by tinting the whole
    /// sprite, because a multiply tint would drag the amber frame towards the order colour.
    /// </summary>
    public sealed class OrderPanelView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer frame;
        [SerializeField] SpriteRenderer swatch;
        [SerializeField] TMP_Text label;
        [SerializeField] float flashSeconds = 0.25f;

        float _flashTimer;
        Color _swatchColor = Color.white;

        public void Show(Color colour, string name)
        {
            _swatchColor = colour;
            _flashTimer = flashSeconds;

            if (frame != null)
                frame.enabled = true;

            if (swatch != null)
            {
                swatch.enabled = true;
                swatch.color = Color.white; // pops white for a frame, then settles on the colour
            }

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = name;
                label.color = colour;
            }
        }

        public void Hide()
        {
            _flashTimer = 0f;

            if (frame != null)
                frame.enabled = false;

            if (swatch != null)
                swatch.enabled = false;

            if (label != null)
                label.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_flashTimer <= 0f || swatch == null)
                return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
                swatch.color = _swatchColor;
        }
    }
}
