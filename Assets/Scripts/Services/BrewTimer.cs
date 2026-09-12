using System;

namespace NightCafe.Services
{
    /// <summary>
    /// The brew timer on the title clock (GDD 6): tapping the clock cycles 1..max minutes and
    /// back to off; when it runs out the device blips, whatever the game is doing. Pure and
    /// clock-agnostic - callers pass the time in seconds.
    /// </summary>
    public sealed class BrewTimer
    {
        readonly int _maxMinutes;
        double _endsAt;

        public BrewTimer(int maxMinutes)
        {
            _maxMinutes = Math.Max(1, maxMinutes);
        }

        /// <summary>Minutes chosen, 0 = off.</summary>
        public int Minutes { get; private set; }

        public bool IsRunning { get; private set; }

        /// <summary>Off -> 1 -> 2 ... -> max -> off. Restarts the countdown on every step.</summary>
        public void Cycle(double now)
        {
            Minutes = Minutes >= _maxMinutes ? 0 : Minutes + 1;
            IsRunning = Minutes > 0;
            _endsAt = now + Minutes * 60.0;
        }

        public void Cancel()
        {
            Minutes = 0;
            IsRunning = false;
        }

        public double Remaining(double now) => IsRunning ? Math.Max(0.0, _endsAt - now) : 0.0;

        /// <summary>True exactly once, on the tick that reaches zero.</summary>
        public bool Tick(double now)
        {
            if (!IsRunning || now < _endsAt)
                return false;

            Cancel();
            return true;
        }

        /// <summary>MM:SS for the segment display; the colon blinks like the clock's.</summary>
        public static string Format(double remainingSeconds, bool colonVisible)
        {
            int total = (int)Math.Ceiling(Math.Max(0.0, remainingSeconds));
            return $"{total / 60:00}{(colonVisible ? ':' : ' ')}{total % 60:00}";
        }
    }
}
