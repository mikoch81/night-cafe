namespace NightCafe.Core
{
    /// <summary>
    /// The four lanes (GDD 2.1). Values double as indices into LaneConfig.lanes.
    /// </summary>
    public enum LanePosition
    {
        LeftUp = 0,
        LeftDown = 1,
        RightUp = 2,
        RightDown = 3
    }

    public static class LanePositionExtensions
    {
        public const int Count = 4;

        public static bool IsLeft(this LanePosition p) =>
            p == LanePosition.LeftUp || p == LanePosition.LeftDown;

        public static bool IsUp(this LanePosition p) =>
            p == LanePosition.LeftUp || p == LanePosition.RightUp;
    }
}
