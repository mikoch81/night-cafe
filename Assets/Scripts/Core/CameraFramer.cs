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

        void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;

            if (_height <= 0)
                return;

            float aspect = (float)_width / _height;
            float distance = DeviceLayout.CameraDistanceFor(aspect, fieldOfView);

            // The body's top face is at z = -thickness (it faces the camera at negative z); aim at
            // the middle of the slab and rise above it by the tilt.
            var target = new Vector3(0f, 0f, -DeviceLayout.BodyThickness * 0.5f);
            float tilt = tiltDegrees * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(0f, Mathf.Sin(tilt), -Mathf.Cos(tilt)) * distance;

            _camera.orthographic = false;
            _camera.fieldOfView = fieldOfView;
            transform.position = target + offset;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }
    }
}
