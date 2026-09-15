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
    /// The painted style hangs a dial behind it: then the hands tell the time and the digits
    /// only come back as the kitchen timer, in place of the face.
    /// </summary>
    public sealed class ClockWidget : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text brewTag;
        [SerializeField] Vector2 hitSize = new(6f, 1.6f);
        [SerializeField] float blinkSeconds = 0.5f;

        [Header("Analogue dial (ART)")]
        [SerializeField] SpriteRenderer face;
        [SerializeField] Transform hourHand;
        [SerializeField] Transform minuteHand;
        [SerializeField] SpriteRenderer[] handInks = new SpriteRenderer[0];

        BrewTimer _timer;
        Func<double> _now;
        float _blinkTimer;
        bool _colonVisible = true;
        bool _analogue;

        public void Initialise(BrewTimer timer, Func<double> now, Vector2 hit)
        {
            _timer = timer;
            _now = now;
            hitSize = hit;
            Render();
        }

        /// <summary>
        /// Screen style: a dial sprite switches the clock to hands (scaled to `width` LCD units);
        /// null goes back to digits. `ink` colours the hands.
        /// </summary>
        public void SetDial(Sprite dial, float width, Color ink)
        {
            _analogue = dial != null && face != null && hourHand != null && minuteHand != null;
            if (face != null)
            {
                face.sprite = dial;
                if (dial != null && dial.bounds.size.x > 0f)
                    face.transform.localScale = Vector3.one * (width / dial.bounds.size.x);
            }

            foreach (SpriteRenderer hand in handInks)
                if (hand != null) hand.color = ink;

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
            DateTime now = DateTime.Now;
            label.text = brewing
                ? BrewTimer.Format(_timer.Remaining(_now()), _colonVisible)
                : ClockFormatter.Format(now, _colonVisible);

            if (brewTag != null)
                brewTag.gameObject.SetActive(brewing);

            // With a dial the digits are the kitchen timer only; the hands step aside for them.
            bool hands = _analogue && !brewing;
            label.enabled = !hands;
            if (face != null)
                face.enabled = hands;
            if (hourHand != null)
            {
                hourHand.gameObject.SetActive(hands);
                hourHand.localRotation = Quaternion.Euler(0f, 0f, -HourAngle(now));
            }
            if (minuteHand != null)
            {
                minuteHand.gameObject.SetActive(hands);
                minuteHand.localRotation = Quaternion.Euler(0f, 0f, -MinuteAngle(now));
            }
        }

        /// <summary>Degrees clockwise from twelve: the hour hand drifts with the minutes.</summary>
        public static float HourAngle(DateTime t) => (t.Hour % 12) * 30f + t.Minute * 0.5f;

        public static float MinuteAngle(DateTime t) => t.Minute * 6f + t.Second * 0.1f;
    }
}
