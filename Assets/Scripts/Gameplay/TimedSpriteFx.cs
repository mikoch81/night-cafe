using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// A sprite shown at a position for a while, or until hidden - the broken cup on a miss,
    /// which stays on the floor until Sablé mops it up (GDD 2.5, review 2026-09-26).
    /// </summary>
    public sealed class TimedSpriteFx : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        float _timer;

        public bool IsBusy => _timer > 0f;

        public Vector2 Position => transform.localPosition;

        /// <summary>Shows the sprite until Hide is called.</summary>
        public void Show(Vector2 localPosition) => Show(localPosition, float.PositiveInfinity);

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
