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

        public void SetStains(int count)
        {
            // A stain disappearing is the cat's doing: fade it instead of snapping it off.
            if (count < _shown && _shown - count == 1 && count < stains.Length)
                BeginWipe(count);
            else
                _wipingIndex = -1;

            _shown = count;
            Render();
        }

        public void ResetAll()
        {
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

        void Render()
        {
            for (int i = 0; i < stains.Length; i++)
            {
                if (stains[i] == null || i == _wipingIndex)
                    continue;

                stains[i].color = i < _shown ? activeColor : inactiveColor;
            }
        }
    }
}
