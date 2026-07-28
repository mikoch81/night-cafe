using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Moves a cup from K1 to K5 (GDD 2.2): linear tween between step points, with the step
    /// duration re-sampled at the start of every step so a tempo change applies immediately.
    /// </summary>
    public sealed class CupController : MonoBehaviour
    {
        IReadOnlyList<Vector2> _steps;
        Func<float> _stepTimeProvider;
        float _stepDuration;
        float _elapsed;
        bool _running;

        public int Lane { get; private set; }

        /// <summary>Index of the step the cup has last reached; 0 = K1.</summary>
        public int StepIndex { get; private set; }

        public event Action<CupController> ReachedCatchPoint;

        public void Launch(int lane, IReadOnlyList<Vector2> steps, Func<float> stepTimeProvider)
        {
            Lane = lane;
            _steps = steps;
            _stepTimeProvider = stepTimeProvider;
            StepIndex = 0;
            _elapsed = 0f;
            _stepDuration = Mathf.Max(0.0001f, stepTimeProvider());
            _running = true;

            transform.localPosition = _steps[0];
            gameObject.SetActive(true);
        }

        public void Despawn()
        {
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

            _elapsed = 0f;
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
