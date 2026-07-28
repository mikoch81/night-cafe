using NightCafe.Config;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Barista Miro. Position changes are instant (GDD 4: an LCD teleport, no tween);
    /// poses swap frame-wise and fall back to the lane's idle pose after a timeout.
    /// </summary>
    public sealed class PlayerPositionController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite trayUp;
        [SerializeField] Sprite trayDown;
        [SerializeField] Sprite catchPose;
        [SerializeField] Sprite missPose;

        LaneConfig _laneConfig;
        float _poseTimer;

        public LanePosition Current { get; private set; } = LanePosition.LeftUp;

        public void Initialise(LaneConfig laneConfig)
        {
            _laneConfig = laneConfig;
            ResetToDefault();
        }

        public void MoveTo(LanePosition position)
        {
            Current = position;
            transform.localPosition = _laneConfig.GetBaristaSlot(position);

            // Art is drawn facing right; the left-hand slots are the mirrored ones.
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (position.IsLeft() ? -1f : 1f);
            transform.localScale = scale;

            ShowIdlePose();
        }

        public void ShowCatchPose(float duration)
        {
            spriteRenderer.sprite = catchPose;
            _poseTimer = duration;
        }

        public void ShowMissPose(float duration)
        {
            spriteRenderer.sprite = missPose;
            _poseTimer = duration;
        }

        public void ResetToDefault()
        {
            _poseTimer = 0f;
            MoveTo(LanePosition.LeftUp);
        }

        /// <summary>Hidden on the title screen so the clock and title text stay readable.</summary>
        public void SetVisible(bool visible)
        {
            spriteRenderer.enabled = visible;
        }

        void Update()
        {
            if (_poseTimer <= 0f)
                return;

            _poseTimer -= Time.deltaTime;
            if (_poseTimer <= 0f)
                ShowIdlePose();
        }

        void ShowIdlePose()
        {
            spriteRenderer.sprite = Current.IsUp() ? trayUp : trayDown;
        }
    }
}
