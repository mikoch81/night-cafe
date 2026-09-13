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
        Sprite _swatchSprite;      // the white pixel the scene was built with
        Vector3 _swatchScale;
        Vector3 _swatchHome;       // where the scene put the swatch's centre
        Sprite[] _cupSprites;      // painted style: a cup per colour instead of a swatch
        Vector2 _cupScale = new(0.55f, 0.55f);

        void Awake()
        {
            if (swatch != null)
            {
                _swatchSprite = swatch.sprite;
                _swatchScale = swatch.transform.localScale;
                _swatchHome = swatch.transform.localPosition;
            }
        }

        /// <summary>Screen style: the panel frame and, for a painted style, the cup sprites (null = swatch).</summary>
        public void SetStyle(Sprite frameSprite, Sprite[] cupSprites, Vector2 cupScale)
        {
            if (frame != null && frameSprite != null)
                frame.sprite = frameSprite;
            _cupSprites = cupSprites;
            _cupScale = cupScale;
        }

        public void Show(int colourIndex, Color colour, string name)
        {
            _swatchColor = colour;
            _flashTimer = flashSeconds;

            if (frame != null)
                frame.enabled = true;

            if (swatch != null)
            {
                swatch.enabled = true;
                bool painted = _cupSprites != null && colourIndex >= 0 && colourIndex < _cupSprites.Length && _cupSprites[colourIndex] != null;
                if (painted)
                {
                    Sprite cup = _cupSprites[colourIndex];
                    swatch.sprite = cup;
                    swatch.transform.localScale = new Vector3(_cupScale.x, _cupScale.y, 1f);
                    // The gameplay cups pivot on their foot (they stand on the shelf); in the
                    // panel the cup's picture is centred where the swatch was, whatever the pivot.
                    Vector3 centre = cup.bounds.center;
                    swatch.transform.localPosition = _swatchHome - new Vector3(centre.x * _cupScale.x, centre.y * _cupScale.y, 0f);
                    _swatchColor = Color.white;
                }
                else if (_swatchSprite != null)
                {
                    swatch.sprite = _swatchSprite;
                    swatch.transform.localScale = _swatchScale;
                    swatch.transform.localPosition = _swatchHome;
                }
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
