using NightCafe.Core;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The four virtual buttons on the shell. GDD 4 asks for 1:1 feedback: tapping a screen
    /// quadrant lights the matching button.
    /// </summary>
    public sealed class DeviceShellView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] buttons = new SpriteRenderer[LanePositionExtensions.Count];
        [SerializeField] Sprite normal;
        [SerializeField] Sprite pressed;
        [SerializeField] float litDuration = 0.10f;

        readonly float[] _timers = new float[LanePositionExtensions.Count];

        public void Press(LanePosition position)
        {
            int index = (int)position;
            if (index < 0 || index >= buttons.Length || buttons[index] == null)
                return;

            buttons[index].sprite = pressed;
            _timers[index] = litDuration;
        }

        public void ResetAll()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                _timers[i] = 0f;
                if (buttons[i] != null)
                    buttons[i].sprite = normal;
            }
        }

        void Update()
        {
            for (int i = 0; i < _timers.Length; i++)
            {
                if (_timers[i] <= 0f)
                    continue;

                _timers[i] -= Time.deltaTime;
                if (_timers[i] <= 0f && buttons[i] != null)
                    buttons[i].sprite = normal;
            }
        }
    }
}
