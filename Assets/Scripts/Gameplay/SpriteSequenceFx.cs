using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Plays a frame list once over a fixed duration, holds the last frame, then fades it out.
    /// The rollover neon cat (GDD 2.7): six frames light up the strokes one after another.
    /// </summary>
    public sealed class SpriteSequenceFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField] float holdSeconds = 0.8f;
        [SerializeField] float fadeSeconds = 0.6f;

        float _duration;
        float _elapsed = -1f;
        Color _baseColor;

        public bool IsPlaying => _elapsed >= 0f;

        void Awake()
        {
            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;
        }

        public void Play(float duration)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
                return;

            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
            spriteRenderer.color = _baseColor;
            spriteRenderer.sprite = frames[0];
            spriteRenderer.enabled = true;
        }

        public void Stop()
        {
            _elapsed = -1f;
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }

        void Update()
        {
            if (!IsPlaying)
                return;

            _elapsed += Time.deltaTime;
            if (_elapsed < _duration)
            {
                int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(_elapsed / _duration * frames.Length));
                spriteRenderer.sprite = frames[index];
                return;
            }

            spriteRenderer.sprite = frames[frames.Length - 1];
            float sinceDone = _elapsed - _duration;
            if (sinceDone < holdSeconds)
                return;

            float fade = 1f - Mathf.Clamp01((sinceDone - holdSeconds) / Mathf.Max(0.01f, fadeSeconds));
            Color c = _baseColor;
            c.a *= fade;
            spriteRenderer.color = c;
            if (fade <= 0f)
                Stop();
        }
    }
}
