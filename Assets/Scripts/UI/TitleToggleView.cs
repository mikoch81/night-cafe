using NightCafe.Core;
using NightCafe.Services;
using TMPro;
using UnityEngine;

namespace NightCafe.UI
{
    /// <summary>
    /// The title-screen settings row: music, sound effects, haptics and the shell skin. The
    /// same switches live in the MENU (PauseMenuView); these are the quick ones on the title.
    /// Only live on Title, so they never swallow a gameplay tap.
    /// </summary>
    public sealed class TitleToggleView : MonoBehaviour
    {
        [SerializeField] TMP_Text musicLabel;
        [SerializeField] TMP_Text soundLabel;
        [SerializeField] TMP_Text hapticsLabel;
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

        /// <summary>Screen style: ink on the painted cards.</summary>
        public void SetColors(Color on, Color off)
        {
            onColor = on;
            offColor = off;
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

            if (Hits(musicLabel, world))
                _settings.ToggleMusic();
            else if (Hits(soundLabel, world))
                _settings.ToggleSfx();
            else if (Hits(hapticsLabel, world))
                _settings.ToggleHaptics();
            else if (Hits(skinLabel, world) && _profile != null)
                _profile.SelectNextSkin();
            else
                return false;

            Render();
            return true;
        }

        public void Render()
        {
            if (_settings == null)
                return;

            SetToggle(musicLabel, "♪", _settings.MusicEnabled);
            SetToggle(soundLabel, "SFX", _settings.SfxEnabled);
            SetToggle(hapticsLabel, "~", _settings.HapticsEnabled);

            if (skinLabel != null && _profile != null)
            {
                SkinCatalog.TryGet(_profile.SelectedSkin, out Skin skin);
                skinLabel.text = Loc.Skin(SkinCatalog.IndexOf(_profile.SelectedSkin), skin.Name);
                skinLabel.color = onColor;
            }
        }

        void SetToggle(TMP_Text label, string glyph, bool on)
        {
            if (label == null)
                return;

            label.text = $"{glyph} {Loc.OnOff(on)}";
            label.color = on ? onColor : offColor;
        }

        bool Hits(TMP_Text label, Vector3 worldPoint) => LcdHit.Hits(label, worldPoint, hitSize);

        /// <summary>
        /// hitSize is authored in the labels' local (LCD) units, the same units the scene
        /// generator lays them out in; the box follows the labels' scale so it can never be
        /// wider than their spacing (which once routed taps to the wrong toggle).
        /// </summary>
        public static Bounds HitBounds(Vector3 centre, Vector3 lossyScale, Vector2 hitSize) =>
            LcdHit.Bounds(centre, lossyScale, hitSize);
    }

    /// <summary>Hit boxes around world-space labels in the LCD scene, shared by the title toggles and the menu.</summary>
    public static class LcdHit
    {
        public static bool Hits(TMP_Text label, Vector3 worldPoint, Vector2 hitSize)
        {
            if (label == null || !label.gameObject.activeInHierarchy)
                return false;

            Vector3 centre = label.transform.position;
            Bounds bounds = Bounds(centre, label.transform.lossyScale, hitSize);
            return bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, centre.z));
        }

        public static Bounds Bounds(Vector3 centre, Vector3 lossyScale, Vector2 hitSize) =>
            new(centre, new Vector3(
                hitSize.x * Mathf.Abs(lossyScale.x),
                hitSize.y * Mathf.Abs(lossyScale.y),
                10f));
    }
}
