using System;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>How Noir gets at a stool (review 2026-09-26).</summary>
    public enum NoirShove
    {
        /// <summary>A shoulder bump: the stool ends up askew.</summary>
        Shoulder,
        /// <summary>Up on his hind legs, front paws on the stool: over it goes.</summary>
        Rear,
        /// <summary>Back to the stool, lashing it with his tail: it shakes (with Miro on top).</summary>
        Tail
    }

    /// <summary>
    /// Noir, the black cat behind the ladder mishaps (1.1.0): he sneaks in along the floor from
    /// the stool's outer side and, once there, asks what to do (<see cref="DecideShove"/> - the
    /// outcome depends on where Miro stands at that moment): a shoulder bump, a push over on his
    /// hind legs, or a few lashes of his tail. The mishap happens with the blow
    /// (<see cref="Bumped"/>); then he saunters back out. Runs on scaled time.
    /// </summary>
    public sealed class BlackCatView : MonoBehaviour
    {
        enum Move
        {
            Hidden,
            SneakingIn,
            Shoving,
            Leaving
        }

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite walkA;
        [SerializeField] Sprite walkB;
        [SerializeField] Sprite bump;
        [SerializeField] Sprite rear;
        [SerializeField] Sprite tailA;
        [SerializeField] Sprite tailB;
        [SerializeField] float floorY = -3.67f;
        [SerializeField] float speed = 4.2f;
        [SerializeField] float leaveSpeed = 3f;
        [SerializeField] float framesPerSecond = 8f;
        [SerializeField] float shoveSeconds = 0.45f;
        [SerializeField] float rearSeconds = 0.7f;
        [Tooltip("The tail lashes this long, at tailLashesPerSecond; the stool starts shaking with the first.")]
        [SerializeField] float tailSeconds = 1.6f;
        [SerializeField] float tailLashesPerSecond = 6f;
        [Tooltip("He stops this far outwards of the stool's centre, where his shoulder meets its leg.")]
        [SerializeField] float stopOffset = 0.75f;
        [SerializeField] float screenHalfWidth = 6.5f;
        [SerializeField] float size = 0.5f;

        Move _move;
        NoirShove _shove;
        float _time;
        float _x;
        float _stopX;
        float _outward;
        bool _bumped;

        public bool IsBusy => _move != Move.Hidden;

        /// <summary>Asked when he reaches the stool: which way to get at it.</summary>
        public Func<NoirShove> DecideShove { get; set; }

        /// <summary>The blow lands: the stool reacts now.</summary>
        public event Action Bumped;

        public void SetFrames(Sprite a, Sprite b, Sprite shove, Sprite reared, Sprite lashA, Sprite lashB, float scale)
        {
            walkA = a != null ? a : walkA;
            walkB = b != null ? b : walkA;
            bump = shove != null ? shove : walkA;
            rear = reared != null ? reared : bump;
            tailA = lashA != null ? lashA : bump;
            tailB = lashB != null ? lashB : tailA;
            size = scale;
        }

        /// <summary>Sneaks in from the `outward` edge (-1 left, +1 right) to the stool at `stoolX`.</summary>
        public void Visit(float stoolX, float outward)
        {
            _outward = Mathf.Sign(outward);
            _stopX = stoolX + _outward * stopOffset;
            _x = _outward * (screenHalfWidth + 0.8f);
            _move = Move.SneakingIn;
            _time = 0f;
            _bumped = false;
            spriteRenderer.enabled = walkA != null;
            Draw(walkA, -_outward, 1f, 0f, Vector2.one);
        }

        public void Hide()
        {
            _move = Move.Hidden;
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
        }

        void Update()
        {
            if (_move == Move.Hidden)
                return;

            float dt = Time.deltaTime;
            _time += dt;
            Sprite walk = Mathf.FloorToInt(_time * framesPerSecond) % 2 == 0 ? walkA : walkB;

            switch (_move)
            {
                case Move.SneakingIn:
                    _x -= _outward * speed * dt;
                    if (_outward * (_x - _stopX) <= 0f)
                    {
                        _x = _stopX;
                        _move = Move.Shoving;
                        _time = 0f;
                        _shove = DecideShove != null ? DecideShove() : NoirShove.Shoulder;
                    }
                    Draw(walk, -_outward, 1f, 0f, Vector2.one);
                    break;
                case Move.Shoving:
                    if (UpdateShove())
                    {
                        _move = Move.Leaving;
                        _time = 0f;
                    }
                    break;
                case Move.Leaving:
                    _x += _outward * leaveSpeed * dt;
                    float edge = Mathf.Abs(_x) - (screenHalfWidth - 0.6f);
                    Draw(walk, _outward, 1f - Mathf.Clamp01(edge / 0.8f), 0f, Vector2.one);
                    if (edge >= 0.8f)
                        Hide();
                    break;
            }
        }

        /// <summary>Plays the blow; true when it is over.</summary>
        bool UpdateShove()
        {
            switch (_shove)
            {
                case NoirShove.Rear:
                {
                    // Rises onto his hind legs, leans in with both paws, lands the push at the top.
                    float t = Mathf.Clamp01(_time / rearSeconds);
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.4f));
                    float push = t < 0.4f ? 0f : Mathf.Sin((t - 0.4f) / 0.6f * Mathf.PI);
                    Land(t >= 0.6f);
                    Draw(rear, -_outward, 1f, -_outward * 0.22f * push, new Vector2(1f, Mathf.Lerp(0.75f, 1f, rise)));
                    return t >= 1f;
                }
                case NoirShove.Tail:
                {
                    // Turns his back on the stool and lashes it with his tail, a grin over his shoulder.
                    float t = Mathf.Clamp01(_time / tailSeconds);
                    bool lashOut = Mathf.FloorToInt(_time * tailLashesPerSecond) % 2 == 0;
                    Land(_time >= 0.5f / tailLashesPerSecond);
                    Draw(lashOut ? tailA : tailB, _outward, 1f, 0f, Vector2.one);
                    return t >= 1f;
                }
                default:
                {
                    // A lean into the stool, the shove at the peak, a lean back.
                    float t = Mathf.Clamp01(_time / shoveSeconds);
                    float lean = Mathf.Sin(t * Mathf.PI);
                    Land(t >= 0.5f);
                    Draw(bump, -_outward, 1f, -_outward * 0.18f * lean, Vector2.one);
                    return t >= 1f;
                }
            }
        }

        void Land(bool now)
        {
            if (_bumped || !now)
                return;

            _bumped = true;
            Bumped?.Invoke();
        }

        /// <summary>`facing` -1/+1 (the art faces right); `nudge` shifts him along x for the blow.</summary>
        void Draw(Sprite sprite, float facing, float alpha, float nudge, Vector2 squash)
        {
            if (sprite != null)
                spriteRenderer.sprite = sprite;
            Transform t = transform;
            t.localPosition = new Vector3(_x + nudge, floorY + 0.03f * Mathf.Abs(Mathf.Sin(_time * 9f)), t.localPosition.z);
            t.localScale = new Vector3(Mathf.Sign(facing) * size * squash.x, size * squash.y, 1f);
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
