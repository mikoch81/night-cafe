using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// Sound and haptics toggles on the title screen. GDD 4 wants haptics switchable "in options"
    /// and there is no options screen yet, so these two glyphs are the whole settings UI for now.
    /// Only live on Title, so they can never swallow a gameplay tap.
    /// </summary>
    public sealed class TitleToggleView : MonoBehaviour
    {
        [SerializeField] TMP_Text soundLabel;
        [SerializeField] TMP_Text hapticsLabel;
        [SerializeField] Vector2 hitSize = new(2.2f, 0.9f);
        [SerializeField] Color onColor = new(1f, 0.788f, 0.4f);
        [SerializeField] Color offColor = new(0.227f, 0.173f, 0.094f);

        SettingsService _settings;
        Camera _camera;

        public void Initialise(SettingsService settings, Camera worldCamera)
        {
            _settings = settings;
            _camera = worldCamera;
            Render();
        }

        /// <summary>Returns true when the tap hit a toggle, so the caller does not also start a round.</summary>
        public bool TryHandleTap(Vector2 screenPosition)
        {
            if (_settings == null || _camera == null || !gameObject.activeInHierarchy)
                return false;

            Vector3 world = _camera.ScreenToWorldPoint(screenPosition);

            if (Hits(soundLabel, world))
            {
                _settings.ToggleSfx();
                if (_settings.SfxEnabled != _settings.MusicEnabled)
                    _settings.ToggleMusic(); // one glyph drives both music and effects
                Render();
                return true;
            }

            if (Hits(hapticsLabel, world))
            {
                _settings.ToggleHaptics();
                Render();
                return true;
            }

            return false;
        }

        public void Render()
        {
            if (_settings == null)
                return;

            if (soundLabel != null)
            {
                soundLabel.text = _settings.SfxEnabled ? "♪ ON" : "♪ OFF";
                soundLabel.color = _settings.SfxEnabled ? onColor : offColor;
            }

            if (hapticsLabel != null)
            {
                hapticsLabel.text = _settings.HapticsEnabled ? "~ ON" : "~ OFF";
                hapticsLabel.color = _settings.HapticsEnabled ? onColor : offColor;
            }
        }

        bool Hits(TMP_Text label, Vector3 worldPoint)
        {
            if (label == null)
                return false;

            Vector3 centre = label.transform.position;
            var bounds = new Bounds(centre, new Vector3(hitSize.x, hitSize.y, 10f));
            return bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, centre.z));
        }
    }
}
