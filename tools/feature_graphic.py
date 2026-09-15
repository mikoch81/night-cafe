#!/usr/bin/env python3
"""Builds the Play feature graphic (1024x500) from the Midjourney window scene (prompt 05).

    py -3.12 tools/feature_graphic.py

The sheet's neon spelled the name wrong and put a borrowed-looking handheld on the counter, so:
the frame is cropped to 2.048:1 without the counter's left end, the lettering inside the neon
frame is covered with a patch of the same rainy window cloned from the same rows to its left (feathered), and
NIGHT CAFÉ is set in Patrick Hand as neon - warm core, amber halo. Pillow only.
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "art", "midjourney", "05_feature.png")
OUT = os.path.join(ROOT, "docs", "store", "feature_graphic.png")
FONT = os.path.join(ROOT, "Assets", "Fonts", "PatrickHand-Regular.ttf")

CROP = (640, 0, 2912, 1109)          # 2272x1109 = 2.048:1, the handheld left of x=640..1450 falls out
SIGN = (1476, 0, 2160, 440)          # inside the neon frame (and the top of its left tube, which the N spilled over)
FRAME_LEFT = (1486, 110, 1506, 440)  # the frame's left tube below the spill, pasted back over the patch
CLONE_DX = -800                      # the same rows of window to the left: rain and dark facades, no letters
CORE = (255, 236, 190)
NEON = (255, 150, 60)


def feathered(size, margin):
    """Soft on three sides; the top edge is the picture's own edge and stays hard."""
    w, h = size
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rectangle((margin, -3 * margin, w - margin, h - margin), fill=255)
    return mask.filter(ImageFilter.GaussianBlur(margin * 0.6))


def main():
    im = Image.open(SRC).convert("RGB")
    x1, y1, x2, y2 = SIGN
    patch = im.crop((x1 + CLONE_DX, y1, x2 + CLONE_DX, y2))
    # match the brightness of the glass between the letters, so the patch does not read as a hole
    target = np.array(im.crop(SIGN)).astype(float)
    src = np.array(patch).astype(float)
    dark_target = np.percentile(target, 30, axis=(0, 1))   # the glass between the letters, not the neon
    gain = np.clip(dark_target / np.maximum(np.percentile(src, 30, axis=(0, 1)), 1), 0.6, 1.4)
    patch = Image.fromarray(np.clip(src * gain, 0, 255).astype(np.uint8))
    original = im.copy()
    im.paste(patch, (x1, y1), feathered(patch.size, 28))
    tube = original.crop(FRAME_LEFT)
    im.paste(tube, FRAME_LEFT[:2], feathered(tube.size, 4))

    # the neon wordmark: glow layers under a bright core, all in the sign's box
    text = "NIGHT CAFÉ"
    font = ImageFont.truetype(FONT, 250)
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    tw = draw.textlength(text, font=font)
    tx, ty = (x1 + x2) / 2 - tw / 2, y1 + 80
    draw.text((tx, ty), text, font=font, fill=NEON + (255,), stroke_width=10, stroke_fill=NEON + (255,))
    halo = layer.filter(ImageFilter.GaussianBlur(28))
    glow = layer.filter(ImageFilter.GaussianBlur(9))
    for extra in (halo, halo, glow):
        im.paste(extra, (0, 0), extra)
    core = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(core).text((tx, ty), text, font=font, fill=CORE + (255,), stroke_width=3, stroke_fill=NEON + (255,))
    im.paste(core, (0, 0), core)

    out = im.crop(CROP).resize((1024, 500), Image.LANCZOS)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    out.save(OUT)
    print(f"{OUT} {out.size}")


if __name__ == "__main__":
    main()
