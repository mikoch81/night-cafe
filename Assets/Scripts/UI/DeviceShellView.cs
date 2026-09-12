using NightCafe.Core;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The shell hardware: four virtual buttons that light on a press (GDD 4, 1:1 feedback),
    /// the A/B mode lever (GDD 5.1) and the skin tint (GDD 6).
    /// </summary>
    public sealed class DeviceShellView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] SpriteRenderer[] buttons = new SpriteRenderer[LanePositionExtensions.Count];
        [SerializeField] Sprite normal;
        [SerializeField] Sprite pressed;
        [SerializeField] float litDuration = 0.10f;

        [Header("Mode lever")]
        [SerializeField] Transform leverTrack;
        [SerializeField] Transform leverKnob;
        [Tooltip("Knob X for Mode A; Mode B mirrors it.")]
        [SerializeField] float leverKnobX = -0.64f;
        [SerializeField] Vector2 leverHitSize = new(3.6f, 1.3f);

        [Header("Skin")]
        [SerializeField] SpriteRenderer shell;
        [SerializeField] Camera worldCamera;
        [SerializeField] Color woodBackground = new(0.329f, 0.188f, 0.102f);
        [Tooltip("Rendered shell per skin id; a skin without one falls back to tinting the default sprite")]
        [SerializeField] SkinShell[] skinShells = System.Array.Empty<SkinShell>();

        [System.Serializable]
        public struct SkinShell
        {
            public string id;
            public Sprite shell;
        }

        readonly float[] _timers = new float[LanePositionExtensions.Count];

        public void Press(LanePosition position)
        {
            int index = (int)position;
            if (index < 0 || index >= buttons.Length || buttons[index] == null)
                return;

            buttons[index].sprite = pressed;
            _timers[index] = litDuration;
        }

        public void ResetAll()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                _timers[i] = 0f;
                if (buttons[i] != null)
                    buttons[i].sprite = normal;
            }
        }

        /// <summary>Slides the knob to the A or B end of its track.</summary>
        public void SetMode(GameMode mode)
        {
            if (leverKnob == null)
                return;

            Vector3 position = leverKnob.localPosition;
            position.x = mode == GameMode.A ? leverKnobX : -leverKnobX;
            leverKnob.localPosition = position;
        }

        /// <summary>True when a world-space tap landed on the lever track.</summary>
        public bool LeverHit(Vector3 worldPoint)
        {
            if (leverTrack == null)
                return false;

            Vector3 centre = leverTrack.position;
            var bounds = new Bounds(centre, new Vector3(leverHitSize.x, leverHitSize.y, 10f));
            return bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, centre.z));
        }

        /// <summary>
        /// Tints the wood and the camera clear colour together, so the finish continues
        /// past the sprite edge on phones wider than the shell art.
        /// </summary>
        public void ApplySkin(in Skin skin)
        {
            if (shell != null)
            {
                Sprite rendered = FindShell(skin.Id);
                if (rendered != null)
                {
                    shell.sprite = rendered;
                    shell.color = Color.white;
                }
                else
                {
                    Sprite fallback = FindShell(SkinCatalog.DefaultId);
                    if (fallback != null)
                        shell.sprite = fallback;
                    shell.color = skin.ShellTint;
                }
            }

            if (worldCamera != null)
                worldCamera.backgroundColor = FindShell(skin.Id) != null ? skin.Background : woodBackground * skin.ShellTint;
        }

        Sprite FindShell(string id)
        {
            for (int i = 0; i < skinShells.Length; i++)
            {
                if (skinShells[i].id == id)
                    return skinShells[i].shell;
            }

            return null;
        }

        void Update()
        {
            for (int i = 0; i < _timers.Length; i++)
            {
                if (_timers[i] <= 0f)
                    continue;

                _timers[i] -= Time.deltaTime;
                if (_timers[i] <= 0f && buttons[i] != null)
                    buttons[i].sprite = normal;
            }
        }
    }
}
