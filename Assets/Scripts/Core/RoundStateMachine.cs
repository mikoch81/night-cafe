namespace NightCafe.Core
{
    /// <summary>
    /// The flow timings from DeviceConfig. The breather length is per mode and is passed in
    /// when a breather starts, so the machine does not have to be rebuilt when the lever flips.
    /// </summary>
    public readonly struct RoundTimings
    {
        public readonly float AttractDelay;
        public readonly float AttractMaxDuration;
        public readonly float GameOverIdleSeconds;
        public readonly float GameOverRestartLockout;

        public RoundTimings(float attractDelay, float attractMaxDuration, float gameOverIdleSeconds,
            float gameOverRestartLockout)
        {
            AttractDelay = attractDelay;
            AttractMaxDuration = attractMaxDuration;
            GameOverIdleSeconds = gameOverIdleSeconds;
            GameOverRestartLockout = gameOverRestartLockout;
        }
    }

    /// <summary>What the presenter has to do next; at most one per call.</summary>
    public enum RoundTransition
    {
        None,
        EnterTitle,
        EnterDemo,
        StartRound,
        EnterGameOver,
        ResumePlaying
    }

    /// <summary>
    /// The Title -> Playing -> Breather -> GameOver flow plus the attract demo, as a plain class:
    /// it decides transitions from time and presses, and the controller carries them out and
    /// confirms with the Entered* calls. Nothing here touches Unity, so every path - the demo
    /// timing out, a press during the game over lockout, a breather ending - is a unit test.
    /// </summary>
    public sealed class RoundStateMachine
    {
        readonly RoundTimings _timings;
        float _idleSince;
        float _gameOverAt;
        float _demoEndsAt;
        float _breatherEndsAt;

        public RoundStateMachine(in RoundTimings timings)
        {
            _timings = timings;
        }

        public GameState State { get; private set; } = GameState.Title;

        /// <summary>True while the attract pilot plays; the state is then Playing or Breather.</summary>
        public bool IsDemo { get; private set; }

        /// <summary>Lane presses move the barista only in a live round the player is playing.</summary>
        public bool AcceptsLaneMoves => (State is GameState.Playing or GameState.Breather) && !IsDemo;

        /// <summary>Title-screen widgets (toggles, lever) may take a press only here.</summary>
        public bool TitleUiActive => State == GameState.Title && !IsDemo;

        public RoundTransition Tick(float now)
        {
            switch (State)
            {
                case GameState.Title:
                    return now - _idleSince >= _timings.AttractDelay ? RoundTransition.EnterDemo : RoundTransition.None;

                case GameState.GameOver:
                    // Tap restarts; leaving it alone returns to the title, the only place the lever works.
                    return now - _gameOverAt >= _timings.GameOverIdleSeconds ? RoundTransition.EnterTitle : RoundTransition.None;

                case GameState.Breather:
                    if (now >= _breatherEndsAt)
                        return RoundTransition.ResumePlaying;
                    break;
            }

            if (IsDemo && now >= _demoEndsAt)
                return RoundTransition.EnterTitle;

            return RoundTransition.None;
        }

        /// <param name="consumedByTitleUi">The press landed on a toggle or the lever (only evaluated on the title).</param>
        public RoundTransition Press(float now, bool consumedByTitleUi)
        {
            if (IsDemo)
                return RoundTransition.StartRound;

            switch (State)
            {
                case GameState.Title:
                    _idleSince = now;
                    return consumedByTitleUi ? RoundTransition.None : RoundTransition.StartRound;

                case GameState.GameOver:
                    // The score, the record line and any unlock deserve to be seen: a tap that
                    // was already in flight when the third stain landed must not skip them.
                    return now - _gameOverAt >= _timings.GameOverRestartLockout
                        ? RoundTransition.StartRound
                        : RoundTransition.None;

                default:
                    return RoundTransition.None;
            }
        }

        /// <summary>The third stain: a real round shows the result, a demo just hands the title back.</summary>
        public RoundTransition PenaltyGameOver() =>
            IsDemo ? RoundTransition.EnterTitle : RoundTransition.EnterGameOver;

        /// <summary>A breather can only interrupt live play; anything else is silently dropped, as before.</summary>
        public bool TryStartBreather(float now, float duration)
        {
            if (State != GameState.Playing)
                return false;

            State = GameState.Breather;
            _breatherEndsAt = now + duration;
            return true;
        }

        public void EnteredTitle(float now)
        {
            IsDemo = false;
            State = GameState.Title;
            _idleSince = now;
        }

        public void EnteredDemo(float now)
        {
            IsDemo = true;
            State = GameState.Playing;
            _demoEndsAt = now + _timings.AttractMaxDuration;
        }

        public void RoundStarted()
        {
            IsDemo = false;
            State = GameState.Playing;
        }

        public void EnteredGameOver(float now)
        {
            State = GameState.GameOver;
            _gameOverAt = now;
        }

        public void ResumedPlaying()
        {
            State = GameState.Playing;
        }
    }
}
