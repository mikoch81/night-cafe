using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// One footstool under an upper barista slot, drawing what LadderService says about it
    /// (1.1.0): it shakes while it creaks, lies on the floor in one piece when knocked over,
    /// lies in pieces when it broke (its own sprite, review 2026-09-26), stands askew when
    /// wobbly, rises halfway on a repair press, and carries Paprika, the ginger stray, lounging
    /// on top. He trots in and jumps up, gets thrown off when shooed (a spin, a landing, a dash
    /// off screen) or hops down when he has had enough.
    /// Runs on scaled time: a paused game freezes it all.
    /// </summary>
    public sealed class LadderView : MonoBehaviour
    {
        enum CatMove
        {
            Hidden,
            JumpingOn,
            Napping,
            Thrown,
            HoppingOff,
            Running
        }

        [SerializeField] SpriteRenderer stool;
        [SerializeField] SpriteRenderer cat;
        [Tooltip("The bar line: where a knocked-over stool lies.")]
        [SerializeField] float floorY = -3.67f;
        [Tooltip("-1 for the left stool (it falls outwards, to the left), +1 for the right one.")]
        [SerializeField] float outward = -1f;
        [SerializeField] float creakDegrees = 2.5f;
        [SerializeField] float creakHz = 11f;
        [SerializeField] float askewDegrees = 11f;
        [SerializeField] Color brokenTint = new(0.55f, 0.5f, 0.47f);
        [Tooltip("Nudge of the cat above the seat, in LCD units.")]
        [SerializeField] float seatLift = 0.0f;
        [Tooltip("The broken stool's pieces lie this far inwards of where it stood, in LCD units.")]
        [SerializeField] float brokenInward = 0.55f;

        [Header("Lying on the floor (review 2026-09-26: inwards, at a slant, into the depth)")]
        [Tooltip("Degrees from upright: past 90, so the seat rests on the floor and the legs run up the screen - back into the room.")]
        [SerializeField] float lieDegrees = 108f;
        [Tooltip("Its length along the floor is foreshortened, as if it pointed back into the room.")]
        [SerializeField] float lieForeshorten = 0.62f;
        [Tooltip("A little up the floor = a little further back.")]
        [SerializeField] float lieDepthLift = 0.14f;
        [SerializeField] Color lieTint = new(0.84f, 0.8f, 0.76f);
        [Tooltip("Sorting order while lying: behind the other stool and Miro (both 29-30).")]
        [SerializeField] int lieSortingOrder = 27;

        [Header("The stray's moves")]
        [Tooltip("Run-in along the floor, then the jump up to the seat.")]
        [SerializeField] float catRunInSeconds = 0.3f;
        [SerializeField] float catJumpSeconds = 0.45f;
        [SerializeField] float catJumpPeak = 0.7f;
        [Tooltip("Thrown off: launch speed (outwards, up), gravity and spin.")]
        [SerializeField] Vector2 catThrowVelocity = new(3.4f, 3.2f);
        [SerializeField] float catGravity = 16f;
        [SerializeField] float catSpin = 720f;
        [SerializeField] float catHopSeconds = 0.5f;
        [Tooltip("Dash off screen after landing, LCD units per second (shooed / left on his own).")]
        [SerializeField] float catFleeSpeed = 7f;
        [SerializeField] float catStrollSpeed = 3.5f;
        [SerializeField] float catFramesPerSecond = 10f;
        [Tooltip("Half the LCD width: past this he is gone.")]
        [SerializeField] float screenHalfWidth = 6.5f;

        Vector3 _home;
        Vector3 _homeScale = Vector3.one;
        int _homeOrder;
        bool _homeKnown;
        LadderState _state;
        float _age;
        float _raise;          // 0 = lying, 1 = upright; a repair press lifts it halfway

        Sprite _sleep, _walkA, _walkB, _thrown;
        float _catScale = 1f;
        float _catMovingScale = 1f;
        Sprite _whole, _broken;
        CatMove _catMove;
        float _catTime;
        Vector2 _catPos;
        Vector2 _catVel;
        Vector2 _jumpFrom;
        float _runSpeed;
        bool _shooNext;

        public void SetStool(SpriteRenderer renderer) => stool = renderer;

        /// <summary>
        /// The stray's frames from the screen style: lounging on the seat, two trotting frames,
        /// flung off; `restScale` for the lounging frame, `moveScale` for the rest.
        /// </summary>
        public void SetCat(Sprite rest, Sprite walkA, Sprite walkB, Sprite thrown, float restScale, float moveScale)
        {
            _sleep = rest;
            _walkA = walkA != null ? walkA : rest;
            _walkB = walkB != null ? walkB : _walkA;
            _thrown = thrown != null ? thrown : _walkB;
            _catScale = restScale;
            _catMovingScale = moveScale;
        }

        /// <summary>The stool in pieces (null: the whole one lies on the floor, darkened).</summary>
        public void SetBroken(Sprite broken)
        {
            _broken = broken;
            if (stool != null && stool.sprite != _broken)
                _whole = stool.sprite;
        }

        /// <summary>The next time the stray leaves the stool it is because Miro shooed him: he flies.</summary>
        public void ShooCat() => _shooNext = true;

        public void Show(LadderState state, int repairsLeft, int repairsTotal)
        {
            RememberHome();
            if (state != _state)
                _age = 0f;

            if (state == LadderState.CatOn && _state != LadderState.CatOn)
                StartCat(CatMove.JumpingOn);
            if (_state == LadderState.CatOn && state != LadderState.CatOn)
                StartCat(_shooNext ? CatMove.Thrown : CatMove.HoppingOff);
            _shooNext = false;

            _state = state;
            _raise = state == LadderState.Toppled && repairsTotal > 0
                ? 1f - (float)repairsLeft / repairsTotal
                : 0f;
            Apply();
        }

        public void ResetAll()
        {
            _state = LadderState.Standing;
            _catMove = CatMove.Hidden;
            _shooNext = false;
            if (cat != null)
            {
                cat.enabled = false;
                cat.transform.localRotation = Quaternion.identity;
            }
            if (_homeKnown)
                Apply();
        }

        /// <summary>
        /// The style applier places the stool at start; the view takes that as home the first
        /// time something happens to it, so both agree on where "standing" is.
        /// </summary>
        void RememberHome()
        {
            if (_homeKnown || stool == null)
                return;

            _home = stool.transform.localPosition;
            _homeScale = stool.transform.localScale;
            _homeOrder = stool.sortingOrder;
            _homeKnown = true;
        }

        void Update()
        {
            if (!_homeKnown)
                return;

            _age += Time.deltaTime;
            if (_state is LadderState.Creaking or LadderState.Wobbly)
                Apply();
            if (_catMove != CatMove.Hidden)
                UpdateCat(Time.deltaTime);
        }

        void Apply()
        {
            if (stool == null)
                return;

            Transform t = stool.transform;
            bool inPieces = _state == LadderState.Broken && _broken != null;
            if (_whole != null)
                stool.sprite = inPieces ? _broken : _whole;
            bool lying = _state is LadderState.Broken or LadderState.Toppled;
            stool.color = !lying ? Color.white : inPieces ? lieTint : _state == LadderState.Broken ? brokenTint : lieTint;
            stool.sortingOrder = lying ? lieSortingOrder : _homeOrder;
            t.localScale = _homeScale;
            if (inPieces)
            {
                // The pieces spread inwards, the stool's own side of the room, a little back.
                t.localRotation = Quaternion.identity;
                t.localScale = new Vector3(-outward * Mathf.Abs(_homeScale.x), _homeScale.y, _homeScale.z);
                t.localPosition = new Vector3(_home.x - outward * brokenInward, floorY + lieDepthLift, _home.z);
                return;
            }

            switch (_state)
            {
                case LadderState.Creaking:
                    t.localPosition = _home + new Vector3(0.02f * Mathf.Sin(_age * creakHz * 3.1f), 0f, 0f);
                    t.localRotation = Quaternion.Euler(0f, 0f, creakDegrees * Mathf.Sin(_age * creakHz * 2f * Mathf.PI));
                    break;
                case LadderState.Wobbly:
                    t.localPosition = _home;
                    t.localRotation = Quaternion.Euler(0f, 0f,
                        -outward * askewDegrees + 1.5f * Mathf.Sin(_age * 2.2f * 2f * Mathf.PI));
                    break;
                case LadderState.Broken:
                case LadderState.Toppled:
                    LayDown(t, Mathf.Lerp(lieDegrees, 45f, _raise), Mathf.Lerp(lieForeshorten, 0.85f, _raise));
                    break;
                default:
                    t.localPosition = _home;
                    t.localRotation = Quaternion.identity;
                    break;
            }
        }

        /// <summary>
        /// Tips the stool inwards by `degrees`, shortens it along its length by `foreshorten` (it
        /// points back into the room) and drops it so its lowest point rests a little up the floor.
        /// Inwards, because down below Miro stands on the outer side of his stool.
        /// </summary>
        void LayDown(Transform t, float degrees, float foreshorten)
        {
            // The pivot is the seat at the top: a negative turn for the right stool (positive for
            // the left) swings the legs inwards, towards the middle of the bar. The length is the
            // sprite's y, scaled before the turn.
            t.localScale = new Vector3(_homeScale.x, _homeScale.y * foreshorten, _homeScale.z);
            t.localRotation = Quaternion.Euler(0f, 0f, -outward * degrees);
            t.localPosition = _home;
            float lowest = stool.bounds.min.y;
            t.localPosition = _home + new Vector3(0f, floorY + lieDepthLift - lowest, 0f);
        }

        // The stool's pivot is its seat - where Miro's feet go - and the sleeping cat's pivot is
        // his belly line (the lounging frame is anchored there), so pivot on pivot. The
        // sprite's bounds would not do: the painted stool carries transparent margin.
        Vector2 Seat => (Vector2)_home + new Vector2(0f, seatLift);

        void StartCat(CatMove move)
        {
            if (cat == null)
                return;

            _catMove = move;
            _catTime = 0f;
            switch (move)
            {
                case CatMove.JumpingOn:
                    // From off to the side, along the floor, towards the stool's foot.
                    _catPos = new Vector2(Seat.x + outward * 2.4f, floorY);
                    _jumpFrom = new Vector2(Seat.x + outward * 0.8f, floorY);
                    break;
                case CatMove.Thrown:
                    _catPos = Seat;
                    _catVel = new Vector2(outward * catThrowVelocity.x, catThrowVelocity.y);
                    break;
                case CatMove.HoppingOff:
                    _catPos = Seat;
                    _jumpFrom = Seat;
                    break;
            }

            cat.enabled = true;
            DrawCat(_catPos, 0f, _sleep, 1f, -outward, Vector2.one);
        }

        void UpdateCat(float dt)
        {
            _catTime += dt;
            float walkFrame = Mathf.FloorToInt(_catTime * catFramesPerSecond) % 2;
            Sprite walk = walkFrame == 0 ? _walkA : _walkB;

            switch (_catMove)
            {
                case CatMove.JumpingOn:
                {
                    if (_catTime < catRunInSeconds)
                    {
                        float r = _catTime / catRunInSeconds;
                        Vector2 p = Vector2.Lerp(_catPos, _jumpFrom, r);
                        DrawCat(p, 0f, walk, Mathf.Clamp01(r * 4f), -outward, Vector2.one);
                        break;
                    }

                    float j = Mathf.Clamp01((_catTime - catRunInSeconds) / catJumpSeconds);
                    Vector2 arc = Vector2.Lerp(_jumpFrom, Seat, j);
                    arc.y = Mathf.Lerp(_jumpFrom.y, Seat.y, Mathf.Sqrt(j)) + catJumpPeak * Mathf.Sin(j * Mathf.PI);
                    // Stretched on the way up, tucked on the way down.
                    Vector2 stretch = new(1f - 0.12f * Mathf.Sin(j * Mathf.PI), 1f + 0.15f * Mathf.Sin(j * Mathf.PI));
                    DrawCat(arc, outward * 25f * (1f - j) * Mathf.Sin(j * Mathf.PI), _walkB, 1f, -outward, stretch);
                    if (j >= 1f)
                    {
                        _catMove = CatMove.Napping;
                        _catTime = 0f;
                    }
                    break;
                }
                case CatMove.Napping:
                {
                    // Lands with a little squash, then settles and breathes.
                    float settle = Mathf.Clamp01(_catTime / 0.2f);
                    float breath = 1f + 0.025f * Mathf.Sin(_catTime * 2.4f);
                    Vector2 squash = new(1f + 0.12f * (1f - settle), (1f - 0.15f * (1f - settle)) * breath);
                    DrawCat(Seat, 0f, _sleep, 1f, -outward, squash);
                    break;
                }
                case CatMove.Thrown:
                {
                    _catVel.y -= catGravity * dt;
                    _catPos += _catVel * dt;
                    if (_catPos.y <= floorY)
                    {
                        _catPos.y = floorY;
                        StartRunning(catFleeSpeed);
                        break;
                    }

                    DrawCat(_catPos, -outward * catSpin * _catTime, _thrown, 1f, outward, Vector2.one);
                    break;
                }
                case CatMove.HoppingOff:
                {
                    // A stretch, then a lazy hop down to the floor on the outer side.
                    float h = Mathf.Clamp01(_catTime / catHopSeconds);
                    Vector2 to = new(Seat.x + outward * 1.1f, floorY);
                    Vector2 p = Vector2.Lerp(_jumpFrom, to, h);
                    p.y = Mathf.Lerp(_jumpFrom.y, to.y, h * h) + 0.4f * Mathf.Sin(h * Mathf.PI);
                    DrawCat(p, -outward * 15f * Mathf.Sin(h * Mathf.PI), _walkA, 1f, outward, Vector2.one);
                    if (h >= 1f)
                    {
                        _catPos = to;
                        StartRunning(catStrollSpeed);
                    }
                    break;
                }
                case CatMove.Running:
                {
                    _catPos.x += outward * _runSpeed * dt;
                    float landing = Mathf.Clamp01(_catTime / 0.15f);
                    Vector2 squash = new(1f + 0.15f * (1f - landing), 1f - 0.2f * (1f - landing));
                    float edge = Mathf.Abs(_catPos.x) - (screenHalfWidth - 0.6f);
                    DrawCat(_catPos + new Vector2(0f, 0.06f * Mathf.Abs(Mathf.Sin(_catTime * 18f))), 0f, walk,
                        1f - Mathf.Clamp01(edge / 0.6f), outward, squash);
                    if (edge >= 0.6f || _catTime > 2f)
                    {
                        _catMove = CatMove.Hidden;
                        cat.enabled = false;
                    }
                    break;
                }
            }
        }

        void StartRunning(float speed)
        {
            _catMove = CatMove.Running;
            _catTime = 0f;
            _runSpeed = speed;
        }

        /// <summary>`facing` is the direction he looks (the walking art faces right).</summary>
        void DrawCat(Vector2 position, float angle, Sprite sprite, float alpha, float facing, Vector2 squash)
        {
            Transform t = cat.transform;
            t.localPosition = new Vector3(position.x, position.y, t.localPosition.z);
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            float size = sprite == _sleep ? _catScale : _catMovingScale;
            t.localScale = new Vector3(Mathf.Sign(facing) * size * squash.x, size * squash.y, 1f);
            if (sprite != null)
                cat.sprite = sprite;
            cat.enabled = cat.sprite != null;
            SetCatAlpha(alpha);
        }

        void SetCatAlpha(float alpha)
        {
            Color c = cat.color;
            c.a = alpha;
            cat.color = c;
        }
    }
}
