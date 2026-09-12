namespace NightCafe.Core
{
    /// <summary>The two shifts (GDD 3). Values index the per-mode config array and the highscore slots.</summary>
    public enum GameMode
    {
        A = 0,
        B = 1
    }

    public static class GameModeExtensions
    {
        public const int Count = 2;

        public static string Letter(this GameMode mode) => mode == GameMode.A ? "A" : "B";

        public static GameMode Next(this GameMode mode) => mode == GameMode.A ? GameMode.B : GameMode.A;
    }
}
