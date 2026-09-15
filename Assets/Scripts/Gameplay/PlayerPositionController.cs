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
        float _moveSeconds;     // presentation hop (ScreenStyle.moveSeconds); the slot itself changes instantly
        float _moveTimer;
        Vector2 _moveFrom;
        Vector2 _moveTo;
        float _poseTimer;
        float _wipeTimer;
        float _wipePeriod;
        bool _idleWipe;     // title screen: wiping the counter with no lane slot

        public LanePosition Current { get; private set; } = LanePosition.LeftUp;

        public void Initialise(LaneConfig laneConfig)
        {
            _laneConfig = laneConfig;
            ResetToDefault();
        }

        public void MoveTo(LanePosition position)
        {
            _wipeTimer = 0f;
            bool changed = position != Current;
            Current = position;
            Vector2 target = _laneConfig.GetBaristaSlot(position) + _offset;
            if (changed && _moveSeconds > 0f && spriteRenderer != null && spriteRenderer.enabled)
            {
                _moveFrom = transform.localPosition;
                _moveTo = target;
                _moveTimer = _moveSeconds;
            }
            else
            {
                _moveTimer = 0f;
                transform.localPosition = target;
            }

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
            _idleWipe = false;
            MoveTo(LanePosition.LeftUp);
        }

        /// <summary>
        /// Title screen (painted style): Miro stands at `feet` off the lanes and wipes the
        /// counter until the round starts (ResetToDefault). `period` = seconds per pose.
        /// </summary>
        public void WipeAt(Vector2 feet, bool faceLeft, float period)
        {
            _poseTimer = 0f;
            _wipeTimer = 0f;
            _moveTimer = 0f;
            _idleWipe = true;
            _wipePeriod = Mathf.Max(0.05f, period);
            transform.localPosition = feet;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceLeft ? -1f : 1f);
            transform.localScale = scale;
            spriteRenderer.sprite = wipePose != null ? wipePose : trayDown;
        }

        /// <summary>Hidden on the title screen so the clock and title text stay readable.</summary>
        public void SetVisible(bool visible)
        {
            spriteRenderer.enabled = visible;
        }

        void Update()
        {
            if (_idleWipe)
            {
                bool wiping = Mathf.FloorToInt(Time.time / _wipePeriod) % 2 == 0;
                spriteRenderer.sprite = wiping && wipePose != null ? wipePose : trayDown;
                return;
            }

            if (_moveTimer > 0f)
            {
                _moveTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_moveTimer / _moveSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                Vector2 p = Vector2.Lerp(_moveFrom, _moveTo, eased);
                p.y += 0.22f * Mathf.Sin(t * Mathf.PI); // a small hop, feet leave the ground
                transform.localPosition = p;
            }

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
            _moveTimer = 0f;
            if (_laneConfig != null)
                transform.localPosition = _laneConfig.GetBaristaSlot(Current) + _offset;
        }

        public void SetMoveSeconds(float seconds) => _moveSeconds = Mathf.Max(0f, seconds);

        /// <summary>Uniform size, keeping the facing (negative x = left-hand slots).</summary>
        public void SetScale(float scale)
        {
            float sign = transform.localScale.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(sign * scale, scale, 1f);
        }
    }
}
