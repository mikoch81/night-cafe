using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Core
{
    /// <summary>
    /// Frames the Bréve Deck so the wood fills the screen width while the LCD stays whole,
    /// on any phone aspect. Re-checks on resize because Android reports the final size late.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFramer : MonoBehaviour
    {
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

            _camera.orthographic = true;
            _camera.orthographicSize = DeviceLayout.OrthographicSizeFor((float)_width / _height);
        }
    }
}
