using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Core
{
    /// <summary>
    /// Places the device camera so the whole Bréve Deck fits the view with a margin on any phone
    /// aspect, pitched a little towards it. Re-checks on resize because Android reports the
    /// final size late.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFramer : MonoBehaviour
    {
        [SerializeField] float fieldOfView = 24f;
        [SerializeField] float tiltDegrees = DeviceLayout.CameraTiltDegrees;

        Camera _camera;
        int _width;
        int _height;
        Vector2 _parallax; // yaw, pitch offsets in degrees (DeviceTilt)

        void Awake()
        {
            _camera = GetComponent<Camera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width == _width && Screen.height == _height)
                return;

            Apply();
        }

        /// <summary>Swings the viewpoint around the device by (yaw, pitch) degrees; the framing distance stays.</summary>
        public void SetParallax(Vector2 degrees)
        {
            if ((degrees - _parallax).sqrMagnitude < 1e-6f)
                return;

            _parallax = degrees;
            Apply();
        }

        void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;

            if (_height <= 0)
                return;

            float aspect = (float)_width / _height;

            // The body's top face is at z = -thickness (it faces the camera at negative z); aim at
            // the middle of the slab from the player's side, a little above it. Parallax pitch
            // swings the viewpoint towards the far side (positive) or the near side.
            var target = new Vector3(0f, 0f, -DeviceLayout.BodyThickness * 0.5f);
            float tilt = tiltDegrees - _parallax.y;
            float distance = DeviceLayout.CameraDistanceFor(aspect, fieldOfView, tilt);
            Vector3 offset = DeviceLayout.CameraOffset(tilt, distance);
            offset = Quaternion.AngleAxis(_parallax.x, Vector3.up) * offset;

            _camera.orthographic = false;
            _camera.fieldOfView = fieldOfView;
            transform.position = target + offset;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }
    }
}
