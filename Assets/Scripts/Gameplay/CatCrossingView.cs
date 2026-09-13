using System;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Sablé mopping across the bottom of the LCD after a miss (GDD 2.5): two frames, 1.6 s.
    /// At high tempo misses can land 0.4 s apart, so a crossing already in flight is left alone
    /// unless the cat mercy wipe forces a fresh one.
    /// </summary>
    public sealed class CatCrossingView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite frameA;
        [SerializeField] Sprite frameB;
        [SerializeField] float duration = 1.6f;
        [SerializeField] float framesPerSecond = 6f;
        [SerializeField] float startX = -6.6f;
        [SerializeField] float endX = 6.6f;
        [SerializeField] float y = -3.4f;

        float _elapsed;
        bool _running;
        float _bob;
        float _tilt;

        public bool IsBusy => _running;

        /// <summary>Fires when a crossing actually starts, so the meow can be rolled for.</summary>
        public event Action CrossingStarted;

        public void Play(bool force = false)
        {
            if (_running && !force)
                return;

            _elapsed = 0f;
            _running = true;
            spriteRenderer.enabled = true;
            Render();
            CrossingStarted?.Invoke();
        }

        public void Stop()
        {
            _running = false;
            spriteRenderer.enabled = false;
        }

        void Update()
        {
            if (!_running)
                return;

            _elapsed += Time.deltaTime;
            if (_elapsed >= duration)
            {
                Stop();
                return;
            }

            Render();
        }

        void Render()
        {
            float t = Mathf.Clamp01(_elapsed / duration);

            // A walk cycle of two frames reads as a shuffle; a little bob and rock (painted style)
            // at twice the frame rate turns it into a trot. Zero in the LCD style.
            float phase = _elapsed * framesPerSecond * Mathf.PI;
            Vector3 position = transform.localPosition;
            position.x = CatPath.XAt(t, startX, endX);
            position.y = y + _bob * Mathf.Abs(Mathf.Sin(phase));
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(0f, 0f, _tilt * Mathf.Sin(phase));

            spriteRenderer.sprite = CatPath.FrameIndexAt(_elapsed, framesPerSecond) == 0 ? frameA : frameB;

            Color color = spriteRenderer.color;
            color.a = CatPath.EdgeAlpha(t);
            spriteRenderer.color = color;
        }

        public void SetFrames(Sprite a, Sprite b)
        {
            frameA = a != null ? a : frameA;
            frameB = b != null ? b : frameB;
        }

        public void SetMotion(float bob, float tilt)
        {
            _bob = bob;
            _tilt = tilt;
            if (!_running)
                transform.localRotation = Quaternion.identity;
        }
    }
}
