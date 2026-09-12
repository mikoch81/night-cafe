using System;
using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// Real wall-clock time on the title screen (GDD 6) - the device pretends to be a night café
    /// gadget sitting on the counter. While the brew timer runs the same digits count it down
    /// and the BREW tag lights up; tapping the clock cycles the timer.
    /// </summary>
    public sealed class ClockWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text brewTag;
        [SerializeField] Vector2 hitSize = new(6f, 1.6f);
        [SerializeField] float blinkSeconds = 0.5f;

        BrewTimer _timer;
        Func<double> _now;
        float _blinkTimer;
        bool _colonVisible = true;

        public void Initialise(BrewTimer timer, Func<double> now, Vector2 hit)
        {
            _timer = timer;
            _now = now;
            hitSize = hit;
            Render();
        }

        /// <summary>Returns true when the tap landed on the clock and cycled the timer.</summary>
        public bool TryHandleTap(Vector3 world)
        {
            if (_timer == null || label == null || !gameObject.activeInHierarchy)
                return false;

            Bounds bounds = TitleToggleView.HitBounds(label.transform.position, label.transform.lossyScale, hitSize);
            if (!bounds.Contains(new Vector3(world.x, world.y, bounds.center.z)))
                return false;

            _timer.Cycle(_now());
            _colonVisible = true;
            _blinkTimer = 0f;
            Render();
            return true;
        }

        void OnEnable()
        {
            _colonVisible = true;
            _blinkTimer = 0f;
            Render();
        }

        void Update()
        {
            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer < blinkSeconds)
            {
                if (_timer != null && _timer.IsRunning)
                    Render(); // seconds tick faster than the colon
                return;
            }

            _blinkTimer = 0f;
            _colonVisible = !_colonVisible;
            Render();
        }

        void Render()
        {
            if (label == null)
                return;

            bool brewing = _timer != null && _timer.IsRunning;
            label.text = brewing
                ? BrewTimer.Format(_timer.Remaining(_now()), _colonVisible)
                : ClockFormatter.Format(DateTime.Now, _colonVisible);

            if (brewTag != null)
                brewTag.gameObject.SetActive(brewing);
        }
    }
}
