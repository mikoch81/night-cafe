using System;
using NightCafe.Config;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Tempo level and step duration (GDD 2.2).
    /// The catch counter runs from round start and is never reset by a miss.
    /// </summary>
    public sealed class TempoService
    {
        readonly TempoSettings _settings;
        int _catches;
        int _level;

        public TempoService(in TempoSettings settings)
        {
            _settings = settings;
            _level = settings.StartLevel;
        }

        public int Catches => _catches;

        public int Level => _level;

        /// <summary>Rush hour multiplies the step time by this (below 1 = faster); 1 otherwise.</summary>
        public float StepFactor { get; set; } = 1f;

        /// <summary>
        /// Seconds a cup takes to travel one step, floored at MinStepTime - and then scaled by
        /// the rush hour, which is the one thing allowed to go past the floor.
        /// </summary>
        public float StepTime =>
            Mathf.Max(_settings.MinStepTime, _settings.BaseStepTime - _settings.StepTimeDecrement * _level) * StepFactor;

        public event Action<int> LevelChanged;

        public void RegisterCatch()
        {
            _catches++;

            int newLevel = Mathf.Min(
                _settings.MaxLevel,
                _settings.StartLevel + _catches / _settings.CatchesPerLevel);

            if (newLevel == _level)
                return;

            _level = newLevel;
            LevelChanged?.Invoke(_level);
        }

        public void Reset()
        {
            _catches = 0;
            bool changed = _level != _settings.StartLevel;
            _level = _settings.StartLevel;

            if (changed)
                LevelChanged?.Invoke(_level);
        }
    }
}
