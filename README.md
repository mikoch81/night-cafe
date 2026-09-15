# Night Café

A one-screen arcade game for Android in the manner of the old LCD handhelds: barista **Miro**
catches coffee cups sliding down four counters and hands them over the bar; three spills and the
shift is over. Cat **Sablé** mops up. The game runs on a virtual handheld, the *Bréve Deck* — a
real 3D walnut-and-aluminium body whose screen shows a painted night-café diorama (or, once
unlocked, an amber Neo-LCD skin). Two modes: **A** catch everything, **B** serve only the colour
on the order card. Real wall clock on the title screen, with a brew timer as an easter egg.

Design source of truth: [docs/GDD.md](docs/GDD.md). Working notes and history for the
next session: [docs/CLAUDE_CODE_HANDOFF.md](docs/CLAUDE_CODE_HANDOFF.md).

## Building

- **Unity 6000.5.5f1** with the Android module (OpenJDK / SDK / NDK bundled). Do not open the
  project with another editor version.
- Open the project, run **NightCafe → Build Scene Setup** once: it (re)generates the scene, the
  configs, the screen styles, icons and splash from the sources in `Assets/Art`, `Assets/Fonts`
  and `Assets/Settings`. Everything under `Assets/Scenes` and `Assets/Settings` is generated.
- **NightCafe → Build Android APK** – development build to `build/NightCafe.apk`
  (`adb install -r build/NightCafe.apk`).
- **Release AAB for Google Play** – `powershell -File tools/build_release.ps1` with the editor
  closed. It needs `build/keystore.local.json` (the upload key; the file and the key stay out of
  the repo — see `Assets/Scripts/Editor/BuildAndroid.cs`) and drops the `com.unity.pipeline`
  dev-tooling package from the manifest for the duration of the build.
- Tests: Window → General → Test Runner, EditMode (or `unity cmd run_tests editor` with the
  `unity` CLI talking to the open editor).

### Art and audio pipeline (optional; the repo holds the results)

`tools/` rebuilds the generated assets: `shell_model.py` (Blender 5.2, the device model),
`gen_art.py` / `gen_art_v3.py` (Inkscape, vector sprites), `cut_sheet.py` (ImageMagick, cutting
Midjourney sheets), `clear_dial.py` / `clear_marks.py` / `lift_from_shelf.py` (Pillow retouch),
`gen_icon.py` (Inkscape + Pillow, launcher icon and splash), `gen_audio.py` / `prep_audio.py`
(numpy, scipy, soundfile, pyloudnorm). Python 3.12. Prompts and cutting parameters live in
[docs/references/PROMPTS.md](docs/references/PROMPTS.md).

## Licences and sources

- Code: © 2026 Michał Kochaniak (mikoch81). All rights reserved.
- Painted screen art and the title props: generated in Midjourney on a paid plan and cut/retouched
  by the tools above (`art/midjourney/LICENSE.md`). Vector sprites: original work in this repo.
- Audio: lo-fi loop from Suno, effects and rain from ElevenLabs, both on paid plans
  (`art/audio/LICENSE.md`); the RETRO chiptune set is synthesised by `tools/gen_audio.py`.
- Fonts (OFL): Cabin Sketch, Patrick Hand, DSEG7 / DSEG14, Liberation Mono; DejaVu Sans Bold for
  the engraving (`art/fonts/`). Licence texts sit next to the fonts.
- Textures and HDRI: ambientCG and Poly Haven, CC0 (`art/textures/LICENSE.md`).
- The game contains no ads, analytics or third-party SDKs and collects no data
  ([docs/privacy.md](docs/privacy.md)).

The game is an original work in the *catch-the-falling-objects* LCD genre; its characters,
setting, art, sounds and names are its own (the checklist is GDD §7).
