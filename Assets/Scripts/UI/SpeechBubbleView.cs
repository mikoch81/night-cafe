using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// Miro's speech bubble (1.1.0, review 2026-09-26): a paper bubble with a tail, next to his
    /// head on the side of the screen's middle, following him as he moves. It pops in, holds
    /// the line for a few seconds and fades. A new line replaces the old one. Runs on scaled
    /// time, so it waits under the pause menu with everything else.
    /// </summary>
    public sealed class SpeechBubbleView : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer tail;
        [SerializeField] TMP_Text text;

        [Tooltip("From Miro's feet (his pivot) to where the tail points, in LCD units; x towards the middle.")]
        [SerializeField] Vector2 headOffset = new(0.55f, 2.2f);
        [Tooltip("Where the bubble's body sits relative to the tail tip: x towards the middle of the screen.")]
        [SerializeField] Vector2 bodyOffset = new(0f, 0.3f);
        [SerializeField] Vector2 padding = new(0.35f, 0.28f);
        [SerializeField] Vector2 minSize = new(1.6f, 0.8f);
        [Tooltip("Text wraps at this width, in LCD units.")]
        [SerializeField] float maxTextWidth = 3.0f;
        [Tooltip("The bubble stays inside this rectangle (LCD units, centre-origin): the band between the " +
                 "shelf ends and the score board, where no cup ever passes and the stools stay in view.")]
        [SerializeField] Rect safeArea = new(-6.1f, 1.7f, 12.2f, 1.4f);
        [SerializeField] float popSeconds = 0.14f;
        [SerializeField] float fadeSeconds = 0.25f;

        float _age;
        float _hold;
        Vector2 _size;

        public bool IsShowing => body != null && body.enabled;

        void Awake()
        {
            Hide();
        }

        public void SetTarget(Transform barista) => target = barista;

        public void Say(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line) || body == null || text == null)
                return;

            text.text = line;
            Vector2 preferred = text.GetPreferredValues(line, maxTextWidth, 0f);
            float width = Mathf.Min(preferred.x, maxTextWidth);
            text.rectTransform.sizeDelta = new Vector2(width + 0.05f, preferred.y);
            _size = Vector2.Max(minSize, new Vector2(width, preferred.y) + padding * 2f);
            body.size = _size;

            _age = 0f;
            _hold = Mathf.Max(0.5f, seconds);
            SetVisible(true);
            Place();
            Apply(0f);
        }

        public void Hide()
        {
            _hold = 0f;
            SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (body != null) body.enabled = visible;
            if (tail != null) tail.enabled = visible && tail.sprite != null;
            if (text != null) text.enabled = visible;
        }

        void Update()
        {
            if (!IsShowing)
                return;

            _age += Time.deltaTime;
            if (_age >= _hold + fadeSeconds)
            {
                Hide();
                return;
            }

            Place();
            Apply(_age);
        }

        /// <summary>Tail at Miro's head, body towards the middle and up, kept inside the safe area.</summary>
        void Place()
        {
            if (target == null)
                return;

            Vector2 feet = target.localPosition;
            float towardsMiddle = feet.x <= 0f ? 1f : -1f;
            Vector2 tip = feet + new Vector2(towardsMiddle * headOffset.x, headOffset.y);

            Vector2 centre = tip + new Vector2(towardsMiddle * (_size.x * 0.5f - bodyOffset.x), bodyOffset.y + _size.y * 0.5f);
            centre.x = Mathf.Clamp(centre.x, safeArea.xMin + _size.x * 0.5f, safeArea.xMax - _size.x * 0.5f);
            // A bubble taller than the band (a long line wrapped three times) grows upwards: the
            // bottom edge is the one that must stay clear of the cups.
            centre.y = Mathf.Max(safeArea.yMin + _size.y * 0.5f, Mathf.Min(centre.y, safeArea.yMax - _size.y * 0.5f));

            transform.localPosition = new Vector3(centre.x, centre.y, transform.localPosition.z);
            if (tail != null)
            {
                // The tail hangs from the body's bottom edge, pointing back at the head.
                float bottom = -_size.y * 0.5f;
                float x = Mathf.Clamp(tip.x - centre.x, -_size.x * 0.5f + 0.35f, _size.x * 0.5f - 0.35f);
                tail.transform.localPosition = new Vector3(x, bottom, 0f);
                Vector3 s = tail.transform.localScale;
                s.x = Mathf.Abs(s.x) * towardsMiddle; // drawn leaning left, towards a head on the left
                tail.transform.localScale = s;
            }
        }

        /// <summary>A quick overshoot pop, then a fade at the end.</summary>
        void Apply(float age)
        {
            float pop = Mathf.Clamp01(age / popSeconds);
            float scale = pop < 1f ? Mathf.Lerp(0.6f, 1.06f, pop) : Mathf.Lerp(1.06f, 1f, Mathf.Clamp01((age - popSeconds) / 0.08f));
            transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = 1f - Mathf.Clamp01((age - _hold) / fadeSeconds);
            SetAlpha(body, alpha);
            SetAlpha(tail, alpha);
            if (text != null)
                text.alpha = alpha;
        }

        static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }
    }
}
