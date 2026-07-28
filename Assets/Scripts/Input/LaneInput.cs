using System;
using NightCafe.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace NightCafe.InputLayer
{
    /// <summary>
    /// Turns keyboard keys and screen-quadrant taps into lane presses (GDD 4).
    /// Keyboard: W / S for the left lanes, Up / Down arrows for the right lanes - the two-buttons-per-hand
    /// layout of the physical handheld. Touch: the four quadrants of the screen.
    /// </summary>
    public sealed class LaneInput : MonoBehaviour
    {
        public event Action<LanePosition> PositionPressed;

        /// <summary>Any press at all - used to start or restart a round.</summary>
        public event Action AnyPressed;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            ReadKeyboard();
            ReadTouch();
        }

        void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.wKey.wasPressedThisFrame)
                Press(LanePosition.LeftUp);

            if (keyboard.sKey.wasPressedThisFrame)
                Press(LanePosition.LeftDown);

            if (keyboard.upArrowKey.wasPressedThisFrame)
                Press(LanePosition.RightUp);

            if (keyboard.downArrowKey.wasPressedThisFrame)
                Press(LanePosition.RightDown);

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                AnyPressed?.Invoke();
        }

        void ReadTouch()
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                    continue;

                Press(QuadrantOf(touch.screenPosition));
            }
        }

        static LanePosition QuadrantOf(Vector2 screenPosition)
        {
            bool left = screenPosition.x < Screen.width * 0.5f;
            bool up = screenPosition.y >= Screen.height * 0.5f;

            if (left)
                return up ? LanePosition.LeftUp : LanePosition.LeftDown;

            return up ? LanePosition.RightUp : LanePosition.RightDown;
        }

        void Press(LanePosition position)
        {
            PositionPressed?.Invoke(position);
            AnyPressed?.Invoke();
        }
    }
}
