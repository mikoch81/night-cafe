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

            transform.localPosition = _steps[0];
            if (CupSkin.Style != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = CupSkin.Style.CupFor(0);
                spriteRenderer.color = CupSkin.DefaultTint;
                transform.localScale = Vector3.one * CupSkin.Scale;
            }
            // Lean into the slide: a cup travelling left tips its top to the left, and vice versa.
            float direction = _steps[_steps.Count - 1].x < _steps[0].x ? 1f : -1f;
            transform.localRotation = Quaternion.Euler(0f, 0f, direction * tiltDegrees);
            gameObject.SetActive(true);
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
            float t = Mathf.Clamp01(_elapsed / _stepDuration);
            transform.localPosition = Vector2.Lerp(_steps[StepIndex], _steps[StepIndex + 1], t);

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
                ReachedCatchPoint?.Invoke(this);
                return;
            }

            _stepDuration = Mathf.Max(0.0001f, _stepTimeProvider());
        }
    }
}
