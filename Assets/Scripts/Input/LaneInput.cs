using System;
using NightCafe.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace NightCafe.InputLayer
{
    /// <summary>
    /// One press, as the game sees it: which lane it maps to (none for the space bar) and,
    /// for pointer input, where on the screen it landed so the title screen can hit-test it.
    /// </summary>
    public readonly struct Press
    {
        public readonly LanePosition? Lane;
        public readonly Vector2 ScreenPosition;
        public readonly bool HasScreenPosition;

        public Press(LanePosition? lane, Vector2? screenPosition)
        {
            Lane = lane;
            HasScreenPosition = screenPosition.HasValue;
            ScreenPosition = screenPosition ?? new Vector2(-1f, -1f);
        }
    }

    /// <summary>
    /// Turns keyboard keys and screen-quadrant taps into presses (GDD 4).
    /// Keyboard: W / S for the left lanes, Up / Down arrows for the right lanes - the
    /// two-buttons-per-hand layout of the physical handheld; Space / Enter is a bare press.
    /// Touch: the four quadrants of the screen. Mouse: same quadrants, so the title screen
    /// can be exercised in the Editor without the device simulator.
    /// Exactly one press is raised per frame; with several fingers the one that began last wins,
    /// which is deterministic and closer to a handheld than "whatever the OS enumerated last".
    /// </summary>
    public sealed class LaneInput : MonoBehaviour
    {
        public event Action<Press> Pressed;

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
            if (ReadTouch())
                return;

            if (ReadMouse())
                return;

            ReadKeyboard();
        }

        bool ReadTouch()
        {
            Touch? newest = null;
            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began)
                    continue;

                if (newest == null || touch.startTime > newest.Value.startTime)
                    newest = touch;
            }

            if (newest == null)
                return false;

            Vector2 position = newest.Value.screenPosition;
            Raise(new Press(QuadrantOf(position), position));
            return true;
        }

        bool ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return false;

            Vector2 position = mouse.position.ReadValue();
            Raise(new Press(QuadrantOf(position), position));
            return true;
        }

        void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.wKey.wasPressedThisFrame)
                Raise(new Press(LanePosition.LeftUp, null));
            else if (keyboard.sKey.wasPressedThisFrame)
                Raise(new Press(LanePosition.LeftDown, null));
            else if (keyboard.upArrowKey.wasPressedThisFrame)
                Raise(new Press(LanePosition.RightUp, null));
            else if (keyboard.downArrowKey.wasPressedThisFrame)
                Raise(new Press(LanePosition.RightDown, null));
            else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                Raise(new Press(null, null));
        }

        public static LanePosition QuadrantOf(Vector2 screenPosition)
        {
            bool left = screenPosition.x < Screen.width * 0.5f;
            bool up = screenPosition.y >= Screen.height * 0.5f;

            if (left)
                return up ? LanePosition.LeftUp : LanePosition.LeftDown;

            return up ? LanePosition.RightUp : LanePosition.RightDown;
        }

        void Raise(in Press press)
        {
            Pressed?.Invoke(press);
        }
    }
}
