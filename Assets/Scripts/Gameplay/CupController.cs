using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// The current screen style as cups see it. Cups are pooled and instantiated at runtime, so
    /// they read the style from here instead of being wired by the scene generator; null means
    /// "as the prefab was built" (tests, and scenes without a ScreenStyleApplier).
    /// </summary>
    public static class CupSkin
    {
        public static Config.ScreenStyle Style { get; private set; }
        public static Color DefaultTint { get; private set; } = Color.white;
        public static float Scale { get; private set; } = 0.5f;

        public static void Set(Config.ScreenStyle style, Color defaultTint, float scale)
        {
            Style = style;
            DefaultTint = defaultTint;
            Scale = scale;
        }

        public static void Clear() => Style = null;
    }

    /// <summary>
    /// Moves a cup from K1 to K5 (GDD 2.2): linear tween between step points, with the step
    /// duration re-sampled at the start of every step so a tempo change applies immediately.
    /// </summary>
    public sealed class CupController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        static int _nextSerial;

        Color _defaultTint = Color.white;
        IReadOnlyList<Vector2> _steps;
        Func<float> _stepTimeProvider;
        float _stepDuration;
        float _elapsed;
        bool _running;
        float _restAngle;      // lean at rest: the rail's slope, or the LCD's fixed tilt
        float _wobblePhase;    // per launch, so a rack of cups does not rock in unison
        float _travelled;      // seconds sliding, drives the wobble

        public int Lane { get; private set; }

        /// <summary>Index of the step the cup has last reached; 0 = K1.</summary>
        public int StepIndex { get; private set; }

        /// <summary>Mode B colour index (GDD 3); 0 in Mode A.</summary>
        public int Colour { get; private set; }

        /// <summary>
        /// Unique per launch, not per object: pooled cups are reused, and the attract pilot
        /// must not mistake a relaunched cup for the one it already decided to fumble.
        /// </summary>
        public int Serial { get; private set; }

        public event Action<CupController> ReachedCatchPoint;

        public void Launch(int lane, IReadOnlyList<Vector2> steps, Func<float> stepTimeProvider, float tiltDegrees = 0f)
        {
            Lane = lane;
            _steps = steps;
            _stepTimeProvider = stepTimeProvider;
            Serial = ++_nextSerial;
            StepIndex = 0;
            _elapsed = 0f;
            _stepDuration = Mathf.Max(0.0001f, stepTimeProvider());
            _running = true;

            if (CupSkin.Style != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = CupSkin.Style.CupFor(0);
                spriteRenderer.color = CupSkin.DefaultTint;
                transform.localScale = Vector3.one * CupSkin.Scale;
            }

            Vector2 first = _steps[0], last = _steps[_steps.Count - 1];
            if (RidesStraightRail)
            {
                // On the plank the cup stands square to the board, whichever way it slides.
                _restAngle = Mathf.Atan2(last.y - first.y, last.x - first.x) * Mathf.Rad2Deg;
                if (last.x < first.x)
                    _restAngle += 180f;
            }
            else
            {
                // Lean into the slide: a cup travelling left tips its top to the left, and vice versa.
                float direction = last.x < first.x ? 1f : -1f;
                _restAngle = direction * tiltDegrees;
            }

            _wobblePhase = Serial * 1.7f;
            _travelled = 0f;
            Place(first);
            gameObject.SetActive(true);
        }

        static bool RidesStraightRail => CupSkin.Style != null && CupSkin.Style.cupsRideStraightRail;

        /// <summary>
        /// Where the cup's foot is drawn for a logical position on the step path. On a straight
        /// rail the foot is dropped onto the K1-K5 line (K1 and K5 lie on it, so the ends and
        /// the catch match the logic exactly); in between, the bent RETRO path would float it.
        /// </summary>
        public static Vector2 FootFor(Vector2 logical, Vector2 first, Vector2 last, bool straightRail)
        {
            if (!straightRail || Mathf.Approximately(first.x, last.x))
                return logical;

            float t = Mathf.InverseLerp(first.x, last.x, logical.x);
            return new Vector2(logical.x, Mathf.Lerp(first.y, last.y, t));
        }

        void Place(Vector2 logical)
        {
            Vector2 foot = FootFor(logical, _steps[0], _steps[_steps.Count - 1], RidesStraightRail);
            float angle = _restAngle;

            Config.ScreenStyle style = CupSkin.Style;
            if (style != null && _running && (style.cupWobble > 0f || style.cupBob > 0f))
            {
                // Rocking about the foot (the sprite pivot) plus a hop at every rock: a cup
                // skittering down a board, not gliding. Starts and ends at rest.
                float w = Mathf.Sin(_wobblePhase + _travelled * style.cupWobbleHz * 2f * Mathf.PI);
                angle += w * style.cupWobble;
                foot.y += Mathf.Abs(w) * style.cupBob;
            }

            transform.localPosition = foot;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void Awake()
        {
            if (spriteRenderer != null)
                _defaultTint = spriteRenderer.color;
        }

        /// <summary>
        /// Mode B colour. A painted style has a sprite per colour and keeps the tint white; the
        /// monochrome style tints its one white mask with the order colour (GDD 3).
        /// </summary>
        public void Paint(int colour, Color tint)
        {
            Colour = colour;
            if (spriteRenderer == null)
                return;

            if (CupSkin.Style != null && CupSkin.Style.HasPaintedCups)
            {
                spriteRenderer.sprite = CupSkin.Style.CupFor(colour);
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = tint;
            }
        }

        public void Despawn()
        {
            Paint(0, CupSkin.Style != null ? CupSkin.DefaultTint : _defaultTint); // Mode A never paints, so a cup last used in Mode B must not keep its colour
            _running = false;
            _steps = null;
            _stepTimeProvider = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_running)
                return;

            _elapsed += Time.deltaTime;
            _travelled += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _stepDuration);
            Place(Vector2.Lerp(_steps[StepIndex], _steps[StepIndex + 1], t));

            if (t < 1f)
                return;

            // Carry the overshoot into the next step. Zeroing it made every step last a whole
            // number of frames, i.e. up to one frame too long - ~4 % slow at T9 on 60 fps and
            // twice that on a throttled phone, which quietly detuned GDD 2.2.
            _elapsed -= _stepDuration;
            StepIndex++;

            if (StepIndex >= _steps.Count - 1)
            {
                _running = false;
                Place(_steps[StepIndex]); // settle: no wobble on the cup that is caught or drops
                ReachedCatchPoint?.Invoke(this);
                return;
            }

            _stepDuration = Mathf.Max(0.0001f, _stepTimeProvider());
        }
    }
}
