using NightCafe.Core;
using NightCafe.Services;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The shell hardware, now real geometry: four button caps that sink and light on a press
    /// (GDD 4, 1:1 feedback), the A/B mode lever whose knob slides between the ends (GDD 5.1),
    /// and the finish (GDD 6). Caps and knob move along their own local axes, so the device
    /// root may tilt freely (parallax) without breaking the motion.
    /// </summary>
    public sealed class DeviceShellView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] Transform[] caps = new Transform[LanePositionExtensions.Count];
        [SerializeField] Renderer[] capRenderers = new Renderer[LanePositionExtensions.Count];
        [SerializeField] float capTravel = 0.08f;
        [SerializeField] float pressSeconds = 0.04f;
        [SerializeField] float releaseSeconds = 0.09f;
        [SerializeField] float litFadeSeconds = 0.18f;
        [SerializeField] Color litColor = new(1f, 0.55f, 0.15f);

        [Header("Mode lever")]
        [SerializeField] Transform leverKnob;
        [SerializeField] Collider leverCollider;
        [Tooltip("Knob offset from the slot centre; mode A is -x, mode B +x.")]
        [SerializeField] float leverKnobX = 0.9f;
        [SerializeField] float leverSlideSeconds = 0.12f;

        [Header("Finish")]
        [SerializeField] Camera deviceCamera;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] int bodyTopSlot;
        [SerializeField] int bodyEdgeSlot = 1;
        [Tooltip("The marks engraved into the top (brand, A/B): their inlay follows the skin, dark on light wood and light on dark.")]
        [SerializeField] Renderer[] engravings = System.Array.Empty<Renderer>();
        [Tooltip("Body materials per skin id: the wood top, the chamfer/edge band and the engraving inlay.")]
        [SerializeField] SkinMaterials[] skins = System.Array.Empty<SkinMaterials>();

        [System.Serializable]
        public struct SkinMaterials
        {
            public string id;
            public Material top;
            public Material edge;
            public Material inlay;
        }

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        readonly float[] _pressAge = new float[LanePositionExtensions.Count];
        readonly bool[] _pressing = new bool[LanePositionExtensions.Count];
        Vector3[] _capRest;
        Vector3[] _capDown;   // "into the device" (world +Z at rest) in each cap's parent space
        MaterialPropertyBlock _block;

        Vector3 _knobCentre;  // slot centre in the knob's parent space
        Vector3 _knobRight;   // unit vector along the slot
        float _knobFrom, _knobTo, _knobAge;
        bool _knobInitialised;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            _capRest = new Vector3[caps.Length];
            _capDown = new Vector3[caps.Length];
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i] == null)
                    continue;

                // Directions are taken from the world axes while the device is at rest and kept
                // in parent space, so a tilting root (parallax) carries them along.
                _capRest[i] = caps[i].localPosition;
                Transform parent = caps[i].parent;
                _capDown[i] = parent != null ? parent.InverseTransformDirection(Vector3.forward).normalized : Vector3.forward;
            }

            InitialiseKnob();
        }

        /// <summary>
        /// Lazy on purpose: the game loop applies the saved mode from its own Awake, and which
        /// Awake runs first depends on scene order - a SetMode that arrived early used to be
        /// dropped, leaving the knob at A while the HUD said B.
        /// </summary>
        void InitialiseKnob()
        {
            if (_knobInitialised || leverKnob == null)
                return;

            Transform parent = leverKnob.parent;
            _knobRight = parent != null ? parent.InverseTransformDirection(Vector3.right).normalized : Vector3.right;
            // The model exports the knob at the mode A end (screen left), so the slot centre
            // is one leverKnobX to the right.
            _knobCentre = leverKnob.localPosition + _knobRight * leverKnobX;
            _knobFrom = _knobTo = -leverKnobX;
            _knobAge = leverSlideSeconds;
            _knobInitialised = true;
        }

        public void Press(LanePosition position)
        {
            int index = (int)position;
            if (index < 0 || index >= caps.Length || caps[index] == null)
                return;

            _pressing[index] = true;
            _pressAge[index] = 0f;
        }

        public void ResetAll()
        {
            for (int i = 0; i < caps.Length; i++)
            {
                _pressing[i] = false;
                _pressAge[i] = 0f;
                ApplyCap(i, 0f, 0f);
            }
        }

        /// <summary>Slides the knob to the A or B end of its slot.</summary>
        public void SetMode(GameMode mode)
        {
            InitialiseKnob();
            if (!_knobInitialised)
                return;

            float target = mode == GameMode.A ? -leverKnobX : leverKnobX;
            if (Mathf.Approximately(target, _knobTo))
                return;

            _knobFrom = KnobX();
            _knobTo = target;
            _knobAge = 0f;
        }

        /// <summary>True when a ray from the device camera lands on the lever.</summary>
        public bool LeverHit(Ray ray) =>
            leverCollider != null && leverCollider.Raycast(ray, out _, 200f);

        /// <summary>Swaps the body's wood and edge materials for the skin; the counter colour follows.</summary>
        public void ApplySkin(in Skin skin)
        {
            if (deviceCamera != null)
                deviceCamera.backgroundColor = skin.Background;

            if (bodyRenderer == null)
                return;

            string id = skin.Id;
            int index = System.Array.FindIndex(skins, entry => entry.id == id);
            if (index < 0)
                index = System.Array.FindIndex(skins, entry => entry.id == SkinCatalog.DefaultId);
            if (index < 0)
                return;

            Material[] materials = bodyRenderer.sharedMaterials;
            if (bodyTopSlot >= materials.Length || bodyEdgeSlot >= materials.Length)
                return;

            if (skins[index].top != null) materials[bodyTopSlot] = skins[index].top;
            if (skins[index].edge != null) materials[bodyEdgeSlot] = skins[index].edge;
            bodyRenderer.sharedMaterials = materials;

            if (skins[index].inlay == null)
                return;
            foreach (Renderer engraving in engravings)
            {
                if (engraving != null)
                    engraving.sharedMaterial = skins[index].inlay;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < caps.Length; i++)
            {
                if (!_pressing[i])
                    continue;

                _pressAge[i] += dt;
                float age = _pressAge[i];
                float depth;
                if (age < pressSeconds)
                    depth = age / Mathf.Max(0.001f, pressSeconds);
                else if (age < pressSeconds + releaseSeconds)
                    depth = 1f - (age - pressSeconds) / Mathf.Max(0.001f, releaseSeconds);
                else
                    depth = 0f;

                float lit = 1f - Mathf.Clamp01((age - pressSeconds) / Mathf.Max(0.001f, litFadeSeconds));
                ApplyCap(i, depth, lit);

                if (depth <= 0f && lit <= 0f)
                    _pressing[i] = false;
            }

            if (_knobInitialised && _knobAge < leverSlideSeconds)
            {
                _knobAge += dt;
                float t = Mathf.Clamp01(_knobAge / Mathf.Max(0.001f, leverSlideSeconds));
                leverKnob.localPosition = _knobCentre + _knobRight * Mathf.LerpUnclamped(_knobFrom, _knobTo, EaseOutBack(t));
            }
        }

        void ApplyCap(int i, float depth, float lit)
        {
            if (caps[i] == null)
                return;

            caps[i].localPosition = _capRest[i] + _capDown[i] * (capTravel * depth);

            if (capRenderers[i] == null)
                return;

            capRenderers[i].GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, litColor * lit);
            capRenderers[i].SetPropertyBlock(_block);
        }

        float KnobX()
        {
            float t = Mathf.Clamp01(_knobAge / Mathf.Max(0.001f, leverSlideSeconds));
            return Mathf.LerpUnclamped(_knobFrom, _knobTo, EaseOutBack(t));
        }

        /// <summary>A physical switch overshoots its end stop a touch before settling.</summary>
        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
