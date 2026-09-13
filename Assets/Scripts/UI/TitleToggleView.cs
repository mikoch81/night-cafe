using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The title-screen settings row: sound, haptics, segment ghosts and the shell skin.
    /// GDD 4 wants haptics switchable "in options" and there is no options screen, so these
    /// glyphs are the whole settings UI. Only live on Title, so they never swallow a gameplay tap.
    /// </summary>
    public sealed class TitleToggleView : MonoBehaviour
    {
        [SerializeField] TMP_Text soundLabel;
        [SerializeField] TMP_Text hapticsLabel;
        [SerializeField] TMP_Text ghostsLabel;
        [SerializeField] TMP_Text skinLabel;
        [SerializeField] Vector2 hitSize = new(2.4f, 0.9f);
        [SerializeField] Color onColor = new(1f, 0.788f, 0.4f);
        [SerializeField] Color offColor = new(0.227f, 0.173f, 0.094f);

        SettingsService _settings;
        ProfileService _profile;

        public void Initialise(SettingsService settings, ProfileService profile)
        {
            _settings = settings;
            _profile = profile;
            Render();
        }

        /// <summary>
        /// Returns true when the tap hit a toggle, so the caller does not also start a round.
        /// `world` is the tap already mapped into the LCD scene (LcdPointer).
        /// </summary>
        public bool TryHandleTap(Vector3 world)
        {
            if (_settings == null || !gameObject.activeInHierarchy)
                return false;

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

            if (Hits(ghostsLabel, world))
            {
                _settings.ToggleGhosts();
                Render();
                return true;
            }

            if (Hits(skinLabel, world) && _profile != null)
            {
                _profile.SelectNextSkin();
                Render();
                return true;
            }

            return false;
        }

        public void Render()
        {
            if (_settings == null)
                return;

            SetToggle(soundLabel, "♪", _settings.SfxEnabled);
            SetToggle(hapticsLabel, "~", _settings.HapticsEnabled);
            SetToggle(ghostsLabel, "░", _settings.GhostsEnabled);

            if (skinLabel != null && _profile != null)
            {
                SkinCatalog.TryGet(_profile.SelectedSkin, out Skin skin);
                skinLabel.text = skin.Name;
                skinLabel.color = onColor;
            }
        }

        void SetToggle(TMP_Text label, string glyph, bool on)
        {
            if (label == null)
                return;

            label.text = on ? $"{glyph} ON" : $"{glyph} OFF";
            label.color = on ? onColor : offColor;
        }

        bool Hits(TMP_Text label, Vector3 worldPoint)
        {
            if (label == null)
                return false;

            Vector3 centre = label.transform.position;
            Bounds bounds = HitBounds(centre, label.transform.lossyScale, hitSize);
            return bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, centre.z));
        }

        /// <summary>
        /// hitSize is authored in the labels' local (LCD) units, the same units the scene
        /// generator lays them out in; the box follows the labels' scale so it can never be
        /// wider than their spacing (which once routed taps to the wrong toggle).
        /// </summary>
        public static Bounds HitBounds(Vector3 centre, Vector3 lossyScale, Vector2 hitSize) =>
            new(centre, new Vector3(
                hitSize.x * Mathf.Abs(lossyScale.x),
                hitSize.y * Mathf.Abs(lossyScale.y),
                10f));
    }
}
