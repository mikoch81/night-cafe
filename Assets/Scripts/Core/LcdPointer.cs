using NightCafe.Services;
using UnityEngine;

namespace NightCafe.Core
{
    /// <summary>
    /// Translates a touch on the phone into the 2D LCD scene: a ray from the device camera hits
    /// the screen face (a MeshCollider whose UVs span the face), and that UV becomes a point in
    /// front of the orthographic LCD camera. Also hands out plain rays for the physical parts
    /// (the mode lever) that are hit-tested in 3D.
    /// </summary>
    public sealed class LcdPointer : MonoBehaviour
    {
        [SerializeField] Camera deviceCamera;
        [SerializeField] Camera lcdCamera;
        [SerializeField] MeshCollider screenFace;
        [SerializeField] float maxDistance = 200f;

        public Ray ScreenRay(Vector2 screenPosition) =>
            deviceCamera != null ? deviceCamera.ScreenPointToRay(screenPosition) : new Ray(Vector3.zero, Vector3.forward);

        /// <summary>True when the touch lands on the screen face; `lcdPoint` is in the LCD scene's world space.</summary>
        public bool TryLcdPoint(Vector2 screenPosition, out Vector3 lcdPoint)
        {
            lcdPoint = Vector3.zero;
            if (deviceCamera == null || lcdCamera == null || screenFace == null)
                return false;

            Ray ray = ScreenRay(screenPosition);
            if (!screenFace.Raycast(ray, out RaycastHit hit, maxDistance))
                return false;

            Vector2 uv = hit.textureCoord;
            if (!LcdMapping.Inside(uv))
                return false;

            Vector3 centre = lcdCamera.transform.position;
            Vector2 point = LcdMapping.LcdPointFromUv(uv, new Vector2(centre.x, centre.y),
                lcdCamera.orthographicSize, lcdCamera.aspect);
            lcdPoint = new Vector3(point.x, point.y, 0f);
            return true;
        }
    }
}
