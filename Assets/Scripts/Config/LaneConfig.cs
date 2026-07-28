using System;
using System.Collections.Generic;
using NightCafe.Core;
using UnityEngine;

namespace NightCafe.Config
{
    /// <summary>
    /// Geometry of the four lanes (GDD 2.1): five step points per lane plus the barista slot.
    /// World units assume screen_bg.png imported at 100 PPU with a centred pivot.
    /// </summary>
    [CreateAssetMenu(menuName = "NightCafe/Lane Config", fileName = "LaneConfig")]
    public sealed class LaneConfig : ScriptableObject
    {
        public const int StepsPerLane = 5;

        [Serializable]
        public struct Lane
        {
            public Vector2[] steps;
            public Vector2 baristaSlot;
        }

        [Tooltip("Indexed by LanePosition: LeftUp, LeftDown, RightUp, RightDown.")]
        public Lane[] lanes = new Lane[LanePositionExtensions.Count];

        [Header("Generator input - left side only, right lanes mirror X")]
        [Tooltip("K1: rail start at the espresso machine head.")]
        public Vector2 leftUpStart = new(-5.84f, 2.14f);

        [Tooltip("Rail bend point, so generated steps follow the painted rail.")]
        public Vector2 leftUpBend = new(-3.11f, 1.50f);

        [Tooltip("K5: catch point at the rail end.")]
        public Vector2 leftUpEnd = new(-1.95f, 0.91f);

        public Vector2 leftDownStart = new(-5.84f, -0.45f);
        public Vector2 leftDownBend = new(-3.19f, -1.12f);
        public Vector2 leftDownEnd = new(-1.95f, -1.69f);

        [Space]
        [Tooltip("Bottom edge of the ghost slot painted into screen_bg - the barista stands on it.")]
        public Vector2 leftUpSlot = new(-1.16f, -0.04f);

        public Vector2 leftDownSlot = new(-1.16f, -2.64f);

        [Tooltip("The bar line baked into screen_bg; stains sit on it and the cat mops along it.")]
        public float barLineY = -3.67f;

        [Header("Sprite scale (art is authored @4x, tuned down to read on the LCD screen)")]
        public float cupScale = 0.5f;
        public float baristaScale = 0.42f;
        public float machineHeadScale = 0.5f;
        public float brokenCupScale = 0.5f;
        public float stainScale = 0.35f;
        public float catScale = 0.35f;

        public IReadOnlyList<Vector2> GetSteps(LanePosition lane) => lanes[(int)lane].steps;

        public IReadOnlyList<Vector2> GetSteps(int laneIndex) => lanes[laneIndex].steps;

        public Vector2 GetCatchPoint(LanePosition lane)
        {
            Vector2[] steps = lanes[(int)lane].steps;
            return steps[steps.Length - 1];
        }

        public Vector2 GetBaristaSlot(LanePosition lane) => lanes[(int)lane].baristaSlot;

        public Vector2 GetStart(LanePosition lane) => lanes[(int)lane].steps[0];

        [ContextMenu("Auto-Generate Steps")]
        public void AutoGenerateSteps()
        {
            lanes = new Lane[LanePositionExtensions.Count];

            lanes[(int)LanePosition.LeftUp] = BuildLane(leftUpStart, leftUpBend, leftUpEnd, leftUpSlot, false);
            lanes[(int)LanePosition.LeftDown] = BuildLane(leftDownStart, leftDownBend, leftDownEnd, leftDownSlot, false);
            lanes[(int)LanePosition.RightUp] = BuildLane(leftUpStart, leftUpBend, leftUpEnd, leftUpSlot, true);
            lanes[(int)LanePosition.RightDown] = BuildLane(leftDownStart, leftDownBend, leftDownEnd, leftDownSlot, true);
        }

        static Lane BuildLane(Vector2 start, Vector2 bend, Vector2 end, Vector2 slot, bool mirrorX)
        {
            var lane = new Lane
            {
                steps = SampleAlongPolyline(start, bend, end, StepsPerLane),
                baristaSlot = slot
            };

            if (mirrorX)
            {
                for (int i = 0; i < lane.steps.Length; i++)
                    lane.steps[i] = new Vector2(-lane.steps[i].x, lane.steps[i].y);

                lane.baristaSlot = new Vector2(-lane.baristaSlot.x, lane.baristaSlot.y);
            }

            return lane;
        }

        /// <summary>
        /// Places <paramref name="count"/> points at equal arc length along start -> bend -> end,
        /// so cups travel a constant distance per step even though the rail is bent.
        /// </summary>
        static Vector2[] SampleAlongPolyline(Vector2 start, Vector2 bend, Vector2 end, int count)
        {
            float firstLength = Vector2.Distance(start, bend);
            float secondLength = Vector2.Distance(bend, end);
            float totalLength = firstLength + secondLength;

            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float distance = totalLength * i / (count - 1);
                points[i] = distance <= firstLength
                    ? Vector2.Lerp(start, bend, firstLength > 0f ? distance / firstLength : 0f)
                    : Vector2.Lerp(bend, end, secondLength > 0f ? (distance - firstLength) / secondLength : 1f);
            }

            return points;
        }

        void OnValidate()
        {
            if (lanes == null || lanes.Length != LanePositionExtensions.Count)
                lanes = new Lane[LanePositionExtensions.Count];
        }
    }
}
