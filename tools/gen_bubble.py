#!/usr/bin/env python3
"""Miro's speech bubble for the painted screen (1.1.0): a 9-sliced paper body and a tail.

    py -3.12 tools/gen_bubble.py

Writes Assets/Art/screen_v3/speech_bubble.png (borders 80 px, see NightCafeSetup.PaintedBorders)
and speech_tail.png (pivot in SpriteAnchors). Drawn at 4x and downsampled; the paper colour and
ink match the receipt and the cards (ScreenStyle.cardInk). 200 px per LCD unit.
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parent.parent / "Assets" / "Art" / "screen_v3"
SS = 4
PAPER = (247, 238, 218, 255)
INK = (59, 41, 28, 255)
STROKE = 7


def grain(img: Image.Image, seed: int) -> Image.Image:
    """A faint paper grain on the opaque pixels, like the painted props."""
    rng = np.random.default_rng(seed)
    a = np.asarray(img).astype(np.int16)
    noise = rng.normal(0, 3.0, a.shape[:2])
    for c in range(3):
        a[..., c] = np.clip(a[..., c] + noise, 0, 255)
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def body() -> Image.Image:
    w, h, r, inset = 360, 240, 72, 6
    img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    box = [inset * SS, inset * SS, (w - inset) * SS - 1, (h - inset) * SS - 1]
    d.rounded_rectangle(box, radius=r * SS, fill=PAPER, outline=INK, width=STROKE * SS)
    img = img.resize((w, h), Image.LANCZOS)
    return grain(img, 7)


def tail() -> Image.Image:
    # Top edge is the join: paper with no ink, overlapping the body's bottom stroke (16 px).
    w, h = 120, 110
    img = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    tip = (10, 104)
    left_top, right_top = (40, 0), (96, 0)
    # A gently curved wedge: sample both edges as quadratic curves towards the tip.
    def curve(p0, p1, ctrl, n=24):
        pts = []
        for i in range(n + 1):
            t = i / n
            x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * ctrl[0] + t * t * p1[0]
            y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * ctrl[1] + t * t * p1[1]
            pts.append((x * SS, y * SS))
        return pts
    left = curve(left_top, tip, (34, 60))
    right = curve(right_top, tip, (64, 70))
    poly = left + list(reversed(right))
    d.polygon(poly, fill=PAPER)
    d.line(left[1:], fill=INK, width=STROKE * SS, joint="curve")
    d.line(right[1:], fill=INK, width=STROKE * SS, joint="curve")
    d.ellipse([tip[0] * SS - STROKE * SS // 2, tip[1] * SS - STROKE * SS // 2,
               tip[0] * SS + STROKE * SS // 2, tip[1] * SS + STROKE * SS // 2], fill=INK)
    img = img.resize((w, h), Image.LANCZOS)
    return grain(img, 11)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    body().save(OUT / "speech_bubble.png")
    tail().save(OUT / "speech_tail.png")
    print("wrote", OUT / "speech_bubble.png", OUT / "speech_tail.png")


if __name__ == "__main__":
    main()
