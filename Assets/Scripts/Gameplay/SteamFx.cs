using UnityEngine;

namespace NightCafe.Gameplay
{
    /// <summary>
    /// The terrible ten seconds (1.1.0, review 2026-09-26): one espresso machine goes haywire -
    /// it shakes, glows hot and belches steam, puff after puff rising, swelling and fading. A
    /// small pool of sprites, no particle system. Runs on scaled time (pauses with the menu).
    /// </summary>
    public sealed class SteamFx : MonoBehaviour
    {
        [Tooltip("The machine heads, indexed by lane (LanePosition).")]
        [SerializeField] Transform[] machines = new Transform[4];
        [SerializeField] SpriteRenderer[] puffs = new SpriteRenderer[0];
        [SerializeField] float puffsPerSecond = 14f;
        [SerializeField] float puffSeconds = 1.1f;
        [SerializeField] Vector2 puffSize = new(0.25f, 0.8f);
        [SerializeField] float riseSpeed = 1.6f;
        [Tooltip("From the machine's pivot to its steam valve, before mirroring for the right-hand machines.")]
        [SerializeField] Vector2 valveOffset = new(0.15f, 0.45f);
        [SerializeField] float shakeAmount = 0.05f;
        [SerializeField] float shakeHz = 23f;
        [SerializeField] Color hotTint = new(1f, 0.72f, 0.6f);

        readonly float[] _age = new float[16];
        readonly Vector2[] _drift = new Vector2[16];
        readonly float[] _spin = new float[16];
        Vector3[] _homes;
        Color[] _tints;
        int _lane = -1;
        float _left;
        float _since;
        float _emit;
        int _next;
        float _time;

        public bool IsRunning => _lane >= 0;

        /// <summary>The lane's machine erupts for `seconds`.</summary>
        public void Erupt(int lane, float seconds)
        {
            RememberMachines();
            Stop();
            if (lane < 0 || lane >= machines.Length || machines[lane] == null)
                return;

            _lane = lane;
            _left = seconds;
            _since = 0f;
            _emit = 0f;
        }

        public void Stop()
        {
            if (_lane >= 0 && _homes != null)
            {
                machines[_lane].localPosition = _homes[_lane];
                SetTint(_lane, _tints[_lane]);
            }

            _lane = -1;
        }

        /// <summary>Stops at once and clears the air (a new shift, the end of one).</summary>
        public void Clear()
        {
            Stop();
            foreach (SpriteRenderer puff in puffs)
                if (puff != null) puff.enabled = false;
        }

        void RememberMachines()
        {
            if (_homes != null)
                return;

            _homes = new Vector3[machines.Length];
            _tints = new Color[machines.Length];
            for (int i = 0; i < machines.Length; i++)
            {
                if (machines[i] == null) continue;
                _homes[i] = machines[i].localPosition;
                var r = machines[i].GetComponent<SpriteRenderer>();
                _tints[i] = r != null ? r.color : Color.white;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;

            if (_lane >= 0)
            {
                _left -= dt;
                _since += dt;
                Transform machine = machines[_lane];
                // A rattle that grows for the first second, and a hot glow pulsing with it.
                float build = Mathf.Clamp01(_since);
                Vector3 jitter = new(Mathf.Sin(_time * shakeHz * 6.28f), Mathf.Sin(_time * shakeHz * 1.37f * 6.28f), 0f);
                machine.localPosition = _homes[_lane] + jitter * (shakeAmount * build);
                SetTint(_lane, Color.Lerp(_tints[_lane], hotTint, 0.5f + 0.5f * Mathf.Sin(_time * 9f)));

                _emit += puffsPerSecond * dt;
                while (_emit >= 1f)
                {
                    _emit -= 1f;
                    Emit(machine);
                }

                if (_left <= 0f)
                    Stop();
            }

            for (int i = 0; i < puffs.Length && i < _age.Length; i++)
            {
                SpriteRenderer puff = puffs[i];
                if (puff == null || !puff.enabled)
                    continue;

                _age[i] += dt;
                float t = _age[i] / puffSeconds;
                if (t >= 1f)
                {
                    puff.enabled = false;
                    continue;
                }

                Transform p = puff.transform;
                p.localPosition += (Vector3)(_drift[i] * dt) + new Vector3(0f, riseSpeed * dt * (1f - 0.5f * t), 0f);
                p.localRotation = Quaternion.Euler(0f, 0f, _spin[i] * _age[i]);
                float size = Mathf.Lerp(puffSize.x, puffSize.y, Mathf.Sqrt(t));
                p.localScale = new Vector3(size, size, 1f);
                Color c = puff.color;
                c.a = Mathf.Sin(Mathf.Min(1f, t * 4f) * Mathf.PI * 0.5f) * (1f - t) * 0.9f;
                puff.color = c;
            }
        }

        void Emit(Transform machine)
        {
            if (puffs.Length == 0)
                return;

            int i = _next;
            _next = (_next + 1) % Mathf.Min(puffs.Length, _age.Length);
            SpriteRenderer puff = puffs[i];
            if (puff == null)
                return;

            float mirror = Mathf.Sign(machine.localScale.x); // the right-hand machines are mirrored
            Vector3 valve = _homes[_lane] + new Vector3(valveOffset.x * mirror, valveOffset.y, 0f);
            puff.transform.localPosition = valve + (Vector3)(Random.insideUnitCircle * 0.12f);
            _drift[i] = new Vector2(mirror * Random.Range(0.4f, 1.4f), Random.Range(-0.1f, 0.4f));
            _spin[i] = Random.Range(-90f, 90f);
            _age[i] = 0f;
            puff.enabled = true;
        }

        void SetTint(int lane, Color color)
        {
            var r = machines[lane].GetComponent<SpriteRenderer>();
            if (r != null)
                r.color = color;
        }
    }
}
