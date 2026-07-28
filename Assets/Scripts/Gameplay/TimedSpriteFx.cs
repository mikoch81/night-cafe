using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// One-shot sprite flash at a world position - the broken cup on a miss (GDD 2.5).
    /// </summary>
    public sealed class TimedSpriteFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        float _timer;

        public bool IsBusy => _timer > 0f;

        public void Show(Vector2 localPosition, float duration)
        {
            transform.localPosition = localPosition;
            _timer = duration;
            spriteRenderer.enabled = true;
        }

        public void Hide()
        {
            _timer = 0f;
            spriteRenderer.enabled = false;
        }

        void Update()
        {
            if (_timer <= 0f)
                return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
                spriteRenderer.enabled = false;
        }
    }
}
