using System.Collections.Generic;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Fixed-size pool of cups. GDD 2.3 caps the screen at four cups, so no runtime allocation
    /// is needed once the pool is warmed.
    /// </summary>
    public sealed class CupPool
    {
        readonly CupController _prefab;
        readonly Transform _parent;
        readonly Stack<CupController> _idle = new();

        public CupPool(CupController prefab, Transform parent, int warmCount)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < warmCount; i++)
                _idle.Push(Create());
        }

        public CupController Rent()
        {
            CupController cup = _idle.Count > 0 ? _idle.Pop() : Create();
            return cup;
        }

        public void Return(CupController cup)
        {
            cup.Despawn();
            _idle.Push(cup);
        }

        CupController Create()
        {
            CupController cup = Object.Instantiate(_prefab, _parent);
            cup.gameObject.SetActive(false);
            return cup;
        }
    }
}
