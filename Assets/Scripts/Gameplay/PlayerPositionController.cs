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
        [SerializeField] Sprite wipePose;

        LaneConfig _laneConfig;
        Vector2 _offset;
        float _poseTimer;
        float _wipeTimer;
        float _wipePeriod;

        public LanePosition Current { get; private set; } = LanePosition.LeftUp;

        public void Initialise(LaneConfig laneConfig)
        {
            _laneConfig = laneConfig;
            ResetToDefault();
        }

        public void MoveTo(LanePosition position)
        {
            _wipeTimer = 0f;
            Current = position;
            transform.localPosition = _laneConfig.GetBaristaSlot(position) + _offset;

            // Art is drawn facing right; the left-hand slots are the mirrored ones.
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (position.IsLeft() ? -1f : 1f);
            transform.localScale = scale;

            ShowIdlePose();
        }

        public void ShowCatchPose(float duration)
        {
            _wipeTimer = 0f;
            spriteRenderer.sprite = catchPose;
            _poseTimer = duration;
        }

        public void ShowMissPose(float duration)
        {
            _wipeTimer = 0f;
            spriteRenderer.sprite = missPose;
            _poseTimer = duration;
        }

        /// <summary>
        /// Breather (GDD 2.6): wipes his hands on the apron, alternating the wipe and down
        /// poses, for as long as the spawns pause. A catch or miss pose interrupts it.
        /// </summary>
        public void ShowBreather(float duration, float period)
        {
            _wipeTimer = duration;
            _wipePeriod = Mathf.Max(0.05f, period);
            _poseTimer = 0f;
            spriteRenderer.sprite = wipePose != null ? wipePose : trayDown;
        }

        public void ResetToDefault()
        {
            _poseTimer = 0f;
            _wipeTimer = 0f;
            MoveTo(LanePosition.LeftUp);
        }

        /// <summary>Hidden on the title screen so the clock and title text stay readable.</summary>
        public void SetVisible(bool visible)
        {
            spriteRenderer.enabled = visible;
        }

        void Update()
        {
            if (_wipeTimer > 0f)
            {
                _wipeTimer -= Time.deltaTime;
                if (_wipeTimer <= 0f)
                {
                    ShowIdlePose();
                    return;
                }

                bool wiping = Mathf.FloorToInt(_wipeTimer / _wipePeriod) % 2 == 0;
                spriteRenderer.sprite = wiping && wipePose != null ? wipePose : trayDown;
                return;
            }

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

        /// <summary>Screen style swap: every pose at once; null keeps the current sprite.</summary>
        public void SetPoses(Sprite up, Sprite down, Sprite catchSprite, Sprite miss, Sprite wipe)
        {
            trayUp = up != null ? up : trayUp;
            trayDown = down != null ? down : trayDown;
            catchPose = catchSprite != null ? catchSprite : catchPose;
            missPose = miss != null ? miss : missPose;
            wipePose = wipe != null ? wipe : wipePose;
            if (spriteRenderer != null && _laneConfig != null)
                spriteRenderer.sprite = Current.IsUp() ? trayUp : trayDown;
        }

        /// <summary>Screen style: a presentation-only shift from the LaneConfig slot (ScreenStyle.baristaOffset).</summary>
        public void SetOffset(Vector2 offset)
        {
            _offset = offset;
            if (_laneConfig != null)
                transform.localPosition = _laneConfig.GetBaristaSlot(Current) + _offset;
        }

        /// <summary>Uniform size, keeping the facing (negative x = left-hand slots).</summary>
        public void SetScale(float scale)
        {
            float sign = transform.localScale.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(sign * scale, scale, 1f);
        }
    }
}
