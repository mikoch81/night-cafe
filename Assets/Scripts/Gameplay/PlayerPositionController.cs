using NightCafe.Config;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Barista Miro. Position changes are instant (GDD 4: an LCD teleport, no tween);
    /// poses swap frame-wise and fall back to the lane's idle pose after a timeout.
    /// On top of that, short presentation-only actions for the café events (1.1.0): shooing
    /// the cat, lifting a stool, tumbling off a broken one, a shrug at a blocked one. They
    /// move, lean and squash the sprite around where he stands; the slot is never touched.
    /// </summary>
    public sealed class PlayerPositionController : MonoBehaviour
    {
        enum Act
        {
            None,
            Shoo,
            Lift,
            Tumble,
            Shrug
        }

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite trayUp;
        [SerializeField] Sprite trayDown;
        [SerializeField] Sprite catchPose;
        [SerializeField] Sprite missPose;
        [SerializeField] Sprite wipePose;
        [Tooltip("Arms up (1.1.0): shooing, lifting. Falls back to the catch pose.")]
        [SerializeField] Sprite reachPose;
        [Tooltip("Shooing with the tea towel (review 2026-09-26): over his head, then whipped forward.")]
        [SerializeField] Sprite shooPoseA;
        [SerializeField] Sprite shooPoseB;

        [Header("Event actions (1.1.0)")]
        [SerializeField] float shooSeconds = 0.6f;
        [SerializeField] float liftSeconds = 0.45f;
        [SerializeField] float tumbleSeconds = 0.55f;
        [SerializeField] float shrugSeconds = 0.4f;
        [Tooltip("Side-to-side sway while climbing an askew stool, in degrees.")]
        [SerializeField] float climbSway = 6f;

        LaneConfig _laneConfig;
        Vector2 _offset;
        Vector2 _rest;          // where he stands; the hop, the climb and the actions draw around it
        float _facing = 1f;     // art faces right; -1 on the left-hand slots
        float _size = 1f;
        float _moveSeconds;     // presentation hop (ScreenStyle.moveSeconds); the slot itself changes instantly
        float _moveTimer;
        Vector2 _moveFrom;
        Vector2 _moveTo;
        float _poseTimer;
        float _wipeTimer;
        float _wipePeriod;
        bool _idleWipe;     // title screen: wiping the counter with no lane slot
        bool _climbing;     // up an askew ladder; the slot changes when the caller says so
        float _climbSeconds;
        Act _act;
        float _actTime;
        float _actSeconds;
        float _actDir;
        Vector2 _tumbleFrom;

        public LanePosition Current { get; private set; } = LanePosition.LeftUp;

        public void Initialise(LaneConfig laneConfig)
        {
            _laneConfig = laneConfig;
            _size = Mathf.Abs(transform.localScale.y);
            ResetToDefault();
        }

        public void MoveTo(LanePosition position)
        {
            _climbing = false;
            _wipeTimer = 0f;
            _act = Act.None;
            bool changed = position != Current;
            Current = position;
            Vector2 target = SlotOf(position);
            if (changed && _moveSeconds > 0f && spriteRenderer != null && spriteRenderer.enabled)
            {
                _moveFrom = transform.localPosition;
                _moveTo = target;
                _moveTimer = _moveSeconds;
            }
            else
            {
                _moveTimer = 0f;
            }

            _rest = target;
            // Art is drawn facing right; the left-hand slots are the mirrored ones.
            _facing = position.IsLeft() ? -1f : 1f;
            ShowIdlePose();
            Draw(_moveTimer > 0f ? _moveFrom : _rest, 0f, Vector2.one);
        }

        /// <summary>
        /// An askew ladder (1.1.0): Miro is drawn climbing from the foot of `position`'s slot up
        /// to it over `seconds`, while the logical slot stays where it was - the caller moves
        /// him there (MoveTo) when the climb is over, so a cup arriving meanwhile is missed.
        /// </summary>
        public void BeginClimb(LanePosition position, float seconds, float height)
        {
            _wipeTimer = 0f;
            _poseTimer = 0f;
            _act = Act.None;
            Vector2 target = SlotOf(position);
            _moveFrom = target - new Vector2(0f, height);
            _moveTo = target;
            _moveTimer = _climbSeconds = Mathf.Max(0.01f, seconds);
            _climbing = true;
            _rest = _moveFrom;
            _facing = position.IsLeft() ? -1f : 1f;
            spriteRenderer.sprite = trayDown;
            Draw(_moveFrom, 0f, Vector2.one);
        }

        /// <summary>
        /// Shoos Sablé off the stool above him, towards `outward` (-1 left, +1 right): he turns
        /// that way and waves both arms with a couple of little jumps.
        /// </summary>
        public void Shoo(float outward) => StartAct(Act.Shoo, shooSeconds, outward);

        /// <summary>
        /// One heave at a knocked-over stool lying on the `outward` side (-1 left, +1 right):
        /// a crouch, then a push up with his arms raised.
        /// </summary>
        public void Lift(float outward) => StartAct(Act.Lift, liftSeconds, outward);

        /// <summary>
        /// A stool gave way: he is already in `lower` (the slot changes now, as always) and
        /// is drawn falling from where he stood, spinning outwards, landing with a bounce.
        /// </summary>
        public void Fall(LanePosition lower, float outward)
        {
            Vector2 from = transform.localPosition;
            MoveTo(lower);
            _moveTimer = 0f;
            _tumbleFrom = from;
            spriteRenderer.sprite = missPose;
            StartAct(Act.Tumble, tumbleSeconds, outward);
        }

        /// <summary>A press on a stool he cannot use: a quick shake of the head.</summary>
        public void Shrug() => StartAct(Act.Shrug, shrugSeconds, 1f);

        void StartAct(Act act, float seconds, float dir)
        {
            if (_idleWipe || _climbing)
                return;

            _wipeTimer = 0f;
            _act = act;
            _actTime = 0f;
            _actSeconds = Mathf.Max(0.05f, seconds);
            _actDir = dir;
        }

        public void ShowCatchPose(float duration)
        {
            _wipeTimer = 0f;
            if (_act != Act.Tumble)
                _act = Act.None;
            spriteRenderer.sprite = catchPose;
            _poseTimer = duration;
        }

        public void ShowMissPose(float duration)
        {
            _wipeTimer = 0f;
            if (_act != Act.Tumble)
                _act = Act.None;
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
            _act = Act.None;
            spriteRenderer.sprite = wipePose != null ? wipePose : trayDown;
        }

        public void ResetToDefault()
        {
            _poseTimer = 0f;
            _wipeTimer = 0f;
            _idleWipe = false;
            _act = Act.None;
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
            _act = Act.None;
            _idleWipe = true;
            _wipePeriod = Mathf.Max(0.05f, period);
            _rest = feet;
            _facing = faceLeft ? -1f : 1f;
            Draw(feet, 0f, Vector2.one);
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

            if (_moveTimer > 0f && _climbing)
            {
                _moveTimer -= Time.deltaTime;
                float c = 1f - Mathf.Clamp01(_moveTimer / _climbSeconds);
                // Rung by rung: a little stepped, not a glide, and the stool sways under him.
                float stepped = (Mathf.Floor(c * 4f) + Mathf.SmoothStep(0f, 1f, c * 4f % 1f)) / 4f;
                _rest = Vector2.Lerp(_moveFrom, _moveTo, stepped);
                Draw(_rest, climbSway * Mathf.Sin(c * Mathf.PI * 6f) * (1f - c), Vector2.one);
                return;
            }

            Vector2 position = _rest;
            if (_moveTimer > 0f)
            {
                _moveTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_moveTimer / _moveSeconds);
                float eased = 1f - (1f - t) * (1f - t);
                position = Vector2.Lerp(_moveFrom, _moveTo, eased);
                position.y += 0.22f * Mathf.Sin(t * Mathf.PI); // a small hop, feet leave the ground
            }

            if (_act != Act.None)
            {
                UpdateAct(position);
                return;
            }

            Draw(position, 0f, Vector2.one);

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

        void UpdateAct(Vector2 position)
        {
            _actTime += Time.deltaTime;
            float t = Mathf.Clamp01(_actTime / _actSeconds);
            Sprite reach = reachPose != null ? reachPose : catchPose;
            float angle = 0f;
            Vector2 squash = Vector2.one;
            float facing = _facing;

            switch (_act)
            {
                case Act.Shoo:
                    // Turned to the stool, the towel whirling over his head and cracking forward,
                    // two little jumps and a lunge. Without the towel poses: arms flapping.
                    facing = _actDir;
                    bool up = Mathf.FloorToInt(_actTime / 0.12f) % 2 == 0;
                    spriteRenderer.sprite = shooPoseA != null
                        ? (up ? shooPoseA : shooPoseB != null ? shooPoseB : shooPoseA)
                        : (up ? reach : catchPose);
                    position += new Vector2(_actDir * 0.14f * Mathf.Sin(t * Mathf.PI), 0.16f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)));
                    angle = -_actDir * 7f * Mathf.Sin(t * Mathf.PI * 4f);
                    break;
                case Act.Lift:
                    // Crouch (the first third), then heave up with a lean towards the stool.
                    facing = _actDir;
                    bool crouch = t < 0.35f;
                    spriteRenderer.sprite = crouch ? trayDown : reach;
                    float heave = crouch ? 0f : Mathf.Sin((t - 0.35f) / 0.65f * Mathf.PI);
                    squash = crouch
                        ? new Vector2(1f + 0.06f * Mathf.Sin(t / 0.35f * Mathf.PI), 1f - 0.1f * Mathf.Sin(t / 0.35f * Mathf.PI))
                        : new Vector2(1f - 0.03f * heave, 1f + 0.06f * heave);
                    angle = -_actDir * 12f * heave;
                    position.x += _actDir * 0.12f * heave;
                    break;
                case Act.Tumble:
                    // From the stool top down to the floor: gravity, a spin outwards, a bounce.
                    spriteRenderer.sprite = missPose;
                    float fall = Mathf.Clamp01(t / 0.75f);
                    position = new Vector2(
                        Mathf.Lerp(_tumbleFrom.x, _rest.x, fall) + _actDir * 0.35f * Mathf.Sin(fall * Mathf.PI),
                        Mathf.Lerp(_tumbleFrom.y, _rest.y, fall * fall));
                    angle = -_actDir * 40f * Mathf.Sin(fall * Mathf.PI * 0.9f);
                    if (t > 0.75f)
                    {
                        float land = (t - 0.75f) / 0.25f;
                        position.y += 0.18f * Mathf.Sin(land * Mathf.PI);
                        squash = new Vector2(1f + 0.08f * (1f - land), 1f - 0.12f * (1f - land));
                    }
                    break;
                case Act.Shrug:
                    angle = 6f * Mathf.Sin(t * Mathf.PI * 6f) * (1f - t);
                    squash = new Vector2(1f, 1f - 0.04f * Mathf.Sin(t * Mathf.PI));
                    break;
            }

            Draw(position, angle, squash, facing);

            if (t < 1f)
                return;

            _act = Act.None;
            Draw(position, 0f, Vector2.one);
            if (_poseTimer <= 0f)
                ShowIdlePose();
        }

        void Draw(Vector2 position, float angle, Vector2 squash) => Draw(position, angle, squash, _facing);

        void Draw(Vector2 position, float angle, Vector2 squash, float facing)
        {
            transform.localPosition = new Vector3(position.x, position.y, transform.localPosition.z);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(facing * _size * squash.x, _size * squash.y, 1f);
        }

        Vector2 SlotOf(LanePosition position) => _laneConfig.GetBaristaSlot(position) + _offset;

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

        public void SetReachPose(Sprite reach) => reachPose = reach != null ? reach : reachPose;

        public void SetShooPoses(Sprite a, Sprite b)
        {
            shooPoseA = a;
            shooPoseB = b;
        }

        /// <summary>Screen style: a presentation-only shift from the LaneConfig slot (ScreenStyle.baristaOffset).</summary>
        public void SetOffset(Vector2 offset)
        {
            _offset = offset;
            _moveTimer = 0f;
            if (_laneConfig == null)
                return;

            _rest = SlotOf(Current);
            Draw(_rest, 0f, Vector2.one);
        }

        public void SetMoveSeconds(float seconds) => _moveSeconds = Mathf.Max(0f, seconds);

        /// <summary>Uniform size, keeping the facing (negative x = left-hand slots).</summary>
        public void SetScale(float scale)
        {
            _size = Mathf.Abs(scale);
            transform.localScale = new Vector3(_facing * _size, _size, 1f);
        }
    }
}
