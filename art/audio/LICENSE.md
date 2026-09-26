# Audio sources (ART sound set)

Generated on 2026-09-14 by Michał (mikoch81) on his own accounts, from the prompts in
`docs/references/PROMPTS.md` (audio section); nothing was downloaded from a library.

| Files | Service | Terms |
|---|---|---|
| `raw/lofi_loop.wav` | Suno | generated on a paid plan (confirmed by Michał 2026-09-14): output owned by the account holder, commercial use allowed |
| `raw/sfx_*.wav`, `raw/ambience_rain_cafe*.wav` | ElevenLabs Sound Effects | generated on a paid plan (confirmed by Michał 2026-09-14): output owned by the account holder, commercial use allowed |
| `raw/sfx_rush_bell.wav`, `raw/sfx_ladder_*.wav`, `raw/sfx_cat_hiss.wav`, `raw/sfx_level_up.wav`, `raw/ambience_crowd*.wav` | ElevenLabs Sound Effects (`eleven_text_to_sound_v2`, flow "Night Café 1.1.0 SFX") | generated 2026-09-26 on the same paid account for 1.1.0; same terms |
| `raw/sfx_machine_frenzy.wav`, `raw/sfx_machine_frenzy2.wav` (unused take) | ElevenLabs Sound Effects (same flow, converted from mp3 with ffmpeg) | generated 2026-09-26 (the terrible ten seconds); same terms |

`raw/` holds the untouched exports. `tools/prep_audio.py` cuts, loops and normalises them into
`Assets/Audio/Art/`, which is what Unity loads. (The synthesised RETRO chiptune was removed in 1.1.0.)
