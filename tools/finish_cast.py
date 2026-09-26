#!/usr/bin/env python3
"""Finishes the 1.1.0 cast cut by cut_sheet.py and writes it to Assets/Art/screen_v3.

    py -3.12 tools/cut_sheet.py art/generated/20_paprika_sheet.png %TEMP%/cut --prefix paprika --names trot_a trot_b lounge leap --fuzz 8
    py -3.12 tools/cut_sheet.py art/generated/21_noir_sheet.png %TEMP%/cut --prefix noir --names walk_a walk_b bump --fuzz 8
    py -3.12 tools/cut_sheet.py art/generated/22_miro_towel.png %TEMP%/cut --prefix miro --names shoo_a shoo_b --fuzz 6
    py -3.12 tools/cut_sheet.py art/generated/23_noir_poses.png %TEMP%/cut --prefix noir --names rear tail_a tail_b --flip tail_a tail_b --fuzz 8
    py -3.12 tools/finish_cast.py %TEMP%/cut

- Miro with the towel is scaled so the pose without a raised arm is as tall as miro_down.png.
- Paprika (ginger) and Noir (black) are scaled to Sablé's size; Paprika's orange is pulled a little
  towards the painting's warm browns, Noir gets a thin cream rim (a black cat on the dark bar
  floor otherwise disappears - the reason Sablé was tone-reversed).
- Prints each sprite's feet pivot (x = middle of the lowest opaque rows, y = just above the
  trimmed bottom) for SpriteAnchors.PaintedPivots.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Art" / "screen_v3"
CAT_SCALE = 0.7
RIM = (220, 195, 154)  # the sand of Sablé's tone reversal (#dcc39a)


def load(src: Path, name: str) -> Image.Image:
    return Image.open(src / f"{name}.png").convert("RGBA")


def scaled(im: Image.Image, factor: float) -> Image.Image:
    return im.resize((round(im.width * factor), round(im.height * factor)), Image.LANCZOS)


def mute(im: Image.Image, saturation: float = 0.82, warmth: float = 0.94) -> Image.Image:
    """A touch less saturated and a touch darker: the gouache room is not that bright."""
    rgb = im.convert("RGB")
    hsv = np.asarray(rgb.convert("HSV")).astype(np.float32)
    hsv[..., 1] *= saturation
    hsv[..., 2] *= warmth
    out = Image.fromarray(np.clip(hsv, 0, 255).astype(np.uint8), "HSV").convert("RGB")
    out.putalpha(im.getchannel("A"))
    return out


def rim(im: Image.Image, width: int = 4) -> Image.Image:
    """A cream outline outside the silhouette, drawn under the cat."""
    pad = width + 2
    base = Image.new("RGBA", (im.width + 2 * pad, im.height + 2 * pad), (0, 0, 0, 0))
    base.paste(im, (pad, pad))
    alpha = base.getchannel("A").point(lambda a: 255 if a > 90 else 0)
    grown = alpha.filter(ImageFilter.MaxFilter(2 * width + 1)).filter(ImageFilter.GaussianBlur(0.8))
    outline = Image.new("RGBA", base.size, RIM + (0,))
    outline.putalpha(grown)
    outline.alpha_composite(base)
    return outline


def feet_pivot(im: Image.Image) -> tuple[float, float]:
    a = np.asarray(im.getchannel("A")) > 128
    rows = np.where(a.any(axis=1))[0]
    bottom = rows.max()
    band = a[max(0, bottom - 12):bottom + 1]
    xs = np.where(band.any(axis=0))[0]
    x = (xs.min() + xs.max()) / 2 / im.width
    y = (im.height - 1 - bottom + 2) / im.height
    return round(float(x), 4), round(float(y), 4)


def main(src: Path) -> None:
    down_h = Image.open(OUT / "miro_down.png").height
    shoo_b = load(src, "miro_shoo_b")
    miro_factor = down_h / shoo_b.height
    out = {
        "miro_shoo_a": scaled(load(src, "miro_shoo_a"), miro_factor),
        "miro_shoo_b": scaled(shoo_b, miro_factor),
    }
    for name in ("trot_a", "trot_b", "lounge", "leap"):
        out[f"paprika_{name}"] = mute(scaled(load(src, f"paprika_{name}"), CAT_SCALE))
    # rear / tail_a / tail_b: sheet 23 (pushes a stool over on his hind legs, shakes it with his tail).
    for name in ("walk_a", "walk_b", "bump", "rear", "tail_a", "tail_b"):
        if (src / f"noir_{name}.png").exists():
            out[f"noir_{name}"] = rim(scaled(load(src, f"noir_{name}"), CAT_SCALE))

    for name, im in out.items():
        im.save(OUT / f"{name}.png")
        px, py = feet_pivot(im)
        print(f'            {{ "{name}", new Vector2({px:.4f}f, {py:.4f}f) }},  // {im.width}x{im.height}')


if __name__ == "__main__":
    main(Path(sys.argv[1]))
