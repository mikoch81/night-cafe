using System;
using NightCafe.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace NightCafe.InputLayer
{
    /// <summary>
    /// One press, as the game sees it. Keyboard presses carry their lane; pointer presses carry
    /// only where on the phone screen they landed - which button (if any) that is, is decided by
    /// hit-testing the device in 3D (DeviceShellView), so a tap beside a cap does nothing.
    /// </summary>
    public readonly struct Press
    {
        public readonly LanePosition? Lane;
        public readonly Vector2 ScreenPosition;
        public readonly bool HasScreenPosition;

        /// <summary>The Android back gesture, Escape or M: open or close the menu.</summary>
        public readonly bool Menu;

        public Press(LanePosition? lane, Vector2? screenPosition, bool menu = false)
        {
            Lane = lane;
            HasScreenPosition = screenPosition.HasValue;
            ScreenPosition = screenPosition ?? new Vector2(-1f, -1f);
            Menu = menu;
        }

        public Press WithLane(LanePosition? lane) =>
            new(lane, HasScreenPosition ? ScreenPosition : null, Menu);
    }

    /// <summary>
    /// Turns touches, clicks and keys into presses (GDD 4).
    /// Keyboard: W / S for the left lanes, Up / Down arrows for the right lanes - the
    /// two-buttons-per-hand layout of the physical handheld; Space / Enter is a bare press;
    /// Escape (the Android back button) and M are the MENU button.
    /// Touch and mouse: a position only; the game loop maps it onto the device's buttons.
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

            Raise(new Press(null, newest.Value.screenPosition));
            return true;
        }

        bool ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return false;

            Raise(new Press(null, mouse.position.ReadValue()));
            return true;
        }

        void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame)
                Raise(new Press(null, null, menu: true));
            else if (keyboard.wKey.wasPressedThisFrame)
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

        void Raise(in Press press)
        {
            Pressed?.Invoke(press);
        }
    }
}
