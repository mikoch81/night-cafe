#!/usr/bin/env python3
"""A puff of steam for the terrible ten seconds (1.1.0): Assets/Art/screen_v3/steam_puff.png.

    py -3.12 tools/gen_steam.py

A soft cream cloud of overlapping lobes with a faint warm-grey underside and a thin, broken ink
edge, so it sits with the ink-and-flat-fill cast rather than looking like a particle blur.
Drawn at 4x and downsampled; centred pivot (the game spins and scales it).
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / "Assets" / "Art" / "screen_v3" / "steam_puff.png"
SS = 4
W, H = 256, 200
LOBES = [(128, 112, 62), (78, 124, 44), (178, 126, 46), (104, 76, 42), (160, 80, 38), (128, 140, 50)]


def main() -> None:
    mask = Image.new("L", (W * SS, H * SS), 0)
    d = ImageDraw.Draw(mask)
    for x, y, r in LOBES:
        d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(2 * SS))

    a = np.asarray(mask).astype(np.float32) / 255.0
    ys = np.linspace(0.0, 1.0, a.shape[0])[:, None]
    shade = np.clip((ys - 0.45) * 1.6, 0.0, 1.0)             # warmer grey towards the bottom
    rgb = np.zeros(a.shape + (3,), np.float32)
    top, bottom = np.array([250, 244, 232]), np.array([196, 184, 170])
    rgb[:] = top * (1 - shade[..., None]) + bottom * shade[..., None]

    # A broken ink edge: the rim of the mask, thinned out with noise.
    edge = np.asarray(mask.filter(ImageFilter.FIND_EDGES)).astype(np.float32) / 255.0
    rng = np.random.default_rng(3)
    edge *= (rng.random(edge.shape) > 0.35)
    edge = np.asarray(Image.fromarray((edge * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(SS * 0.6))) / 255.0
    ink = np.array([90, 70, 58])
    k = np.clip(edge * 2.2, 0, 0.75)[..., None]
    rgb = rgb * (1 - k) + ink * k

    alpha = np.clip(a * 0.92, 0, 1)
    img = Image.fromarray(np.dstack([rgb, alpha * 255]).astype(np.uint8), "RGBA")
    img = img.resize((W, H), Image.LANCZOS)
    img.save(OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
