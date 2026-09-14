# Audio sources (ART sound set)

Generated on 2026-09-14 by Michał (mikoch81) on his own accounts, from the prompts in
`docs/references/PROMPTS.md` (audio section); nothing was downloaded from a library.

| Files | Service | Terms |
|---|---|---|
| `raw/lofi_loop.wav` | Suno | generated on a paid plan (confirmed by Michał 2026-09-14): output owned by the account holder, commercial use allowed |
| `raw/sfx_*.wav`, `raw/ambience_rain_cafe*.wav` | ElevenLabs Sound Effects | generated on a paid plan (confirmed by Michał 2026-09-14): output owned by the account holder, commercial use allowed |

`raw/` holds the untouched exports. `tools/prep_audio.py` cuts, loops and normalises them into
`Assets/Audio/Art/`, which is what Unity loads. The RETRO set (`Assets/Audio/*.wav`) is
synthesised by `tools/gen_audio.py` and has no third-party source.
