using System;
using NightCafe.Config;
using UnityEngine;

namespace NightCafe.Services
{
    /// <summary>
    /// Stain count and the end-of-shift condition (GDD 2.5).
    /// </summary>
    public sealed class PenaltyService
    {
        readonly int _maxStains;
        int _stains;

        public PenaltyService(int maxStains)
        {
            _maxStains = Mathf.Max(1, maxStains);
        }

        public int Stains => _stains;

        public int MaxStains => _maxStains;

        public bool IsGameOver => _stains >= _maxStains;

        public event Action<int> StainsChanged;
        public event Action GameOverTriggered;

        public void AddStain()
        {
            if (_stains >= _maxStains)
                return;

            _stains++;
            StainsChanged?.Invoke(_stains);

            if (_stains >= _maxStains)
                GameOverTriggered?.Invoke();
        }

        /// <summary>Cat mercy: wipes one stain if there is any. Returns false when the board is clean.</summary>
        public bool TryRemoveStain()
        {
            if (_stains <= 0)
                return false;

            _stains--;
            StainsChanged?.Invoke(_stains);
            return true;
        }

        public void Reset()
        {
            if (_stains == 0)
                return;

            _stains = 0;
            StainsChanged?.Invoke(_stains);
        }
    }
}
