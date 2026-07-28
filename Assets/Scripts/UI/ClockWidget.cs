using System;
using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// Real wall-clock time on the title screen (GDD 6) - the device pretends to be a night café
    /// gadget sitting on the counter.
    /// </summary>
    public sealed class ClockWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] float blinkSeconds = 0.5f;

        float _blinkTimer;
        bool _colonVisible = true;

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
                return;

            _blinkTimer = 0f;
            _colonVisible = !_colonVisible;
            Render();
        }

        void Render()
        {
            if (label != null)
                label.text = ClockFormatter.Format(DateTime.Now, _colonVisible);
        }
    }
}
