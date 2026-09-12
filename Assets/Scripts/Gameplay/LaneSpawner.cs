using System;
using System.Collections.Generic;
using NightCafe.Config;
using NightCafe.Core;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// Owns the cup pool and drives <see cref="SpawnDirector"/> decisions (GDD 2.3).
    /// When a spawn is blocked by the on-screen limit it stays due and fires as soon as a slot frees.
    /// </summary>
    public sealed class LaneSpawner : MonoBehaviour, ILaneOccupancy
    {
        [SerializeField] CupController cupPrefab;
        [SerializeField] Transform cupRoot;
        [SerializeField] int poolWarmCount = 4;

        readonly List<CupController> _active = new(8);

        ModeConfig _config;
        LaneConfig _laneConfig;
        SpawnDirector _director;
        TempoService _tempo;
        CupPool _pool;
        float _nextSpawnAt;

        public bool SpawningEnabled { get; set; }

        /// <summary>Parent of the pooled cups; broken-cup FX must sit under the same transform.</summary>
        public Transform CupRoot => cupRoot;

        public event Action<CupController> CupReachedCatchPoint;

        /// <summary>Raised right after a cup starts down its rail, before its first frame - paint it here.</summary>
        public event Action<CupController> CupSpawned;

        public IReadOnlyList<CupController> ActiveCups => _active;

        public void Initialise(ModeConfig config, LaneConfig laneConfig, TempoService tempo, IRandom rng)
        {
            _config = config;
            _laneConfig = laneConfig;
            _tempo = tempo;
            _director = new SpawnDirector(config.Spawn, rng);
            _pool ??= new CupPool(cupPrefab, cupRoot, poolWarmCount);
        }

        public void BeginRound()
        {
            DespawnAll();
            _nextSpawnAt = Time.time + _director.FirstSpawnDelay;
            SpawningEnabled = true;
        }

        /// <summary>Re-arms the spawn clock after a breather so cups do not burst out at once (GDD 2.6).</summary>
        public void ResumeAfterBreather()
        {
            _nextSpawnAt = Time.time + _director.RollNextInterval(_tempo.StepTime);
            SpawningEnabled = true;
        }

        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                ReturnToPool(_active[i]);

            _active.Clear();
        }

        public void ReturnCup(CupController cup)
        {
            _active.Remove(cup);
            ReturnToPool(cup);
        }

        void Update()
        {
            if (!SpawningEnabled || _director == null)
                return;

            if (Time.time < _nextSpawnAt)
                return;

            if (!_director.TryPickLane(this, _tempo.Level, out int lane))
                return; // Stay due; retry next frame once a slot frees up.

            Spawn(lane);

            // Accumulate from the scheduled time, not from now, so frame overshoot does not
            // stretch every interval. A spawn that was held back by the screen limit for longer
            // than a frame is a real delay though, and re-bases on the present.
            float basis = Mathf.Max(_nextSpawnAt, Time.time - Time.deltaTime);
            _nextSpawnAt = basis + _director.RollNextInterval(_tempo.StepTime);
        }

        void Spawn(int lane)
        {
            CupController cup = _pool.Rent();
            cup.ReachedCatchPoint += OnCupReachedCatchPoint;
            cup.transform.localScale = Vector3.one * _laneConfig.cupScale;
            cup.Launch(lane, _laneConfig.GetSteps(lane), () => _tempo.StepTime, _laneConfig.cupTiltDegrees);
            _active.Add(cup);
            CupSpawned?.Invoke(cup);
        }

        void OnCupReachedCatchPoint(CupController cup)
        {
            CupReachedCatchPoint?.Invoke(cup);
        }

        void ReturnToPool(CupController cup)
        {
            cup.ReachedCatchPoint -= OnCupReachedCatchPoint;
            _pool.Return(cup);
        }

        public int TotalActiveCups => _active.Count;

        public int CupsOnLane(int laneIndex)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Lane == laneIndex)
                    count++;
            }

            return count;
        }

        public int NewestCupStep(int laneIndex)
        {
            int newest = int.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                CupController cup = _active[i];
                if (cup.Lane == laneIndex && cup.StepIndex < newest)
                    newest = cup.StepIndex;
            }

            return newest;
        }
    }
}
