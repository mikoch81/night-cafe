using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCafe.Core
{
    /// <summary>
    /// Parallax: as the phone tilts, the viewpoint drifts a few degrees around the device, the
    /// way a real handheld on a counter would show a different side. Driven by the attitude
    /// sensor on the phone and by the mouse in the editor; the camera is what moves, so the
    /// light and shadows stay put on the device. Off when degrees is zero.
    /// </summary>
    [RequireComponent(typeof(CameraFramer))]
    public sealed class DeviceTilt : MonoBehaviour
    {
        [Tooltip("Largest viewpoint swing in degrees; 0 disables the effect.")]
        [SerializeField] float degrees = 3f;

        [Tooltip("How much phone tilt (degrees) is needed for the full swing.")]
        [SerializeField] float phoneTiltForFullSwing = 25f;

        [SerializeField] float smoothing = 8f;

        CameraFramer _framer;
        Quaternion _reference;
        bool _haveReference;
        Vector2 _current;

        void Awake()
        {
            _framer = GetComponent<CameraFramer>();
        }

        void OnEnable()
        {
            if (AttitudeSensor.current != null)
                InputSystem.EnableDevice(AttitudeSensor.current);
        }

        void Update()
        {
            if (degrees <= 0f)
                return;

            Vector2 target = ReadSensor() ?? ReadMouse() ?? Vector2.zero;
            _current = Vector2.Lerp(_current, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            _framer.SetParallax(_current * degrees);
        }

        /// <summary>Yaw/pitch offset in -1..1 from the phone's attitude relative to where it started.</summary>
        Vector2? ReadSensor()
        {
            AttitudeSensor sensor = AttitudeSensor.current;
            if (sensor == null)
                return null;

            Quaternion attitude = sensor.attitude.ReadValue();
            if (!_haveReference)
            {
                _reference = attitude;
                _haveReference = true;
            }

            // Drift the reference slowly so a new resting angle becomes the new centre.
            _reference = Quaternion.Slerp(_reference, attitude, 0.2f * Time.deltaTime);

            Quaternion delta = Quaternion.Inverse(_reference) * attitude;
            Vector3 euler = delta.eulerAngles;
            float pitch = Mathf.DeltaAngle(0f, euler.x);
            float yaw = Mathf.DeltaAngle(0f, euler.y);
            float scale = 1f / Mathf.Max(1f, phoneTiltForFullSwing);
            return new Vector2(Mathf.Clamp(yaw * scale, -1f, 1f), Mathf.Clamp(-pitch * scale, -1f, 1f));
        }

        static Vector2? ReadMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
                return null;

            Vector2 position = mouse.position.ReadValue();
            return new Vector2(
                Mathf.Clamp(position.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(position.y / Screen.height * 2f - 1f, -1f, 1f));
        }
    }
}
