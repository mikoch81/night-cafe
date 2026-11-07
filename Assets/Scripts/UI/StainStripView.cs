using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The three stain slots on the bar line. Stains live inside the LCD (GDD 5.1 Segments)
    /// rather than on an overlay, so the cat can visibly mop one away.
    /// </summary>
    public sealed class StainStripView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] stains = new SpriteRenderer[3];
        [SerializeField] Color activeColor = new(1f, 0.788f, 0.4f);
        [SerializeField] Color inactiveColor = new(0.227f, 0.173f, 0.094f);
        [SerializeField] float wipeSeconds = 0.35f;

        int _shown;
        int _wipingIndex = -1;
        float _wipeTimer;
        int _pulseIndex = -1;   // 1.2.0: a new stain lands with a splat instead of just appearing
        float _pulseTimer;
        float _scale = -1f;
        const float PulseSeconds = 0.35f;

        public void SetStains(int count)
        {
            // A stain disappearing is the cat's doing: fade it instead of snapping it off.
            if (count < _shown && _shown - count == 1 && count < stains.Length)
                BeginWipe(count);
            else
                _wipingIndex = -1;

            if (count > _shown && count <= stains.Length)
            {
                _pulseIndex = count - 1;
                _pulseTimer = PulseSeconds;
            }

            _shown = count;
            Render();
        }

        public void ResetAll()
        {
            if (_pulseIndex >= 0 && stains[_pulseIndex] != null && _scale > 0f)
                stains[_pulseIndex].transform.localScale = Vector3.one * _scale;
            _pulseIndex = -1;
            _shown = 0;
            _wipingIndex = -1;
            Render();
        }

        void BeginWipe(int index)
        {
            _wipingIndex = index;
            _wipeTimer = wipeSeconds;
        }

        void Update()
        {
            UpdatePulse();
            if (_wipingIndex < 0)
                return;

            _wipeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_wipeTimer / wipeSeconds);
            stains[_wipingIndex].color = Color.Lerp(inactiveColor, activeColor, t);

            if (_wipeTimer <= 0f)
            {
                _wipingIndex = -1;
                Render();
            }
        }

        void UpdatePulse()
        {
            if (_pulseIndex < 0 || stains[_pulseIndex] == null)
                return;

            if (_scale < 0f)
                _scale = stains[_pulseIndex].transform.localScale.y;

            _pulseTimer -= Time.deltaTime;
            float left = Mathf.Clamp01(_pulseTimer / PulseSeconds);
            stains[_pulseIndex].transform.localScale = Vector3.one * (_scale * (1f + 0.7f * left * left));
            if (_pulseTimer <= 0f)
                _pulseIndex = -1;
        }

        void Render()
        {
            for (int i = 0; i < stains.Length; i++)
            {
                if (stains[i] == null || i == _wipingIndex)
                    continue;

                stains[i].color = i < _shown ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// Screen style: the stain sprite, the colour of a stain that is there, the colour of
        /// an empty slot (dim amber on the LCD, fully transparent in the painted style) and size.
        /// </summary>
        public void SetStyle(Sprite sprite, Color active, Color inactive, float scale)
        {
            _scale = scale;
            activeColor = active;
            inactiveColor = inactive;
            foreach (SpriteRenderer stain in stains)
            {
                if (stain == null) continue;
                if (sprite != null) stain.sprite = sprite;
                stain.transform.localScale = Vector3.one * scale;
            }
            Render();
        }
    }
}
