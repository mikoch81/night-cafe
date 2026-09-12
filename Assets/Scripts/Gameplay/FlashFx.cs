using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// A short glow pulse. Used for the bar neon on a combo bonus and on rollover (GDD 2.4, 2.7),
    /// and for the screen dim on game over (GDD 2.5).
    /// </summary>
    public sealed class FlashFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] float duration = 0.35f;
        [SerializeField] float peakAlpha = 0.85f;

        float _timer;
        int _blinksLeft;
        float _blinkInterval;
        float _nextBlinkAt;

        public void Flash()
        {
            _timer = duration;
            spriteRenderer.enabled = true;
            Apply(peakAlpha);
        }

        /// <summary>Repeated pulses - the neon behind the window during the breather (GDD 2.6).</summary>
        public void Blink(int count, float interval)
        {
            Flash();
            _blinksLeft = Mathf.Max(0, count - 1);
            _blinkInterval = interval;
            _nextBlinkAt = Time.time + interval;
        }

        /// <summary>Holds a constant alpha until cleared - the game over dim.</summary>
        public void Hold(float alpha)
        {
            _timer = 0f;
            spriteRenderer.enabled = true;
            Apply(alpha);
        }

        public void Clear()
        {
            _timer = 0f;
            _blinksLeft = 0;
            spriteRenderer.enabled = false;
        }

        void Update()
        {
            if (_blinksLeft > 0 && Time.time >= _nextBlinkAt)
            {
                _blinksLeft--;
                _nextBlinkAt += _blinkInterval;
                Flash();
            }

            if (_timer <= 0f)
                return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Clear();
                return;
            }

            Apply(peakAlpha * (_timer / duration));
        }

        void Apply(float alpha)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
