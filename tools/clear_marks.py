#!/usr/bin/env python3
"""Wipes the loose strokes drawn inside a paper prop (ruled lines, doodles), keeping its outline.

    python3 tools/clear_marks.py Assets/Art/screen_v3/result_card.png

Dark pixels are grouped into connected strokes; the biggest one is the outline and stays,
every other stroke is painted over with the paper colour (the median of the light pixels) and
its edge softened, so lettering can go anywhere on the sheet. Pillow + numpy + scipy.
"""
import argparse

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sprite")
    ap.add_argument("--dark", type=int, default=150, help="luminance below which a pixel is ink")
    ap.add_argument("--keep", type=int, default=1, help="how many of the biggest strokes to keep (the outline)")
    args = ap.parse_args()

    im = Image.open(args.sprite).convert("RGBA")
    rgba = np.array(im)
    opaque = rgba[..., 3] > 8
    lum = rgba[..., :3].astype(int) @ [299, 587, 114] // 1000
    ink = opaque & (lum < args.dark)
    labels, count = ndimage.label(ink, structure=np.ones((3, 3)))
    if count == 0:
        raise SystemExit("no ink found")
    areas = ndimage.sum(ink, labels, index=np.arange(1, count + 1))
    keep = set(int(i) + 1 for i in np.argsort(areas)[::-1][:args.keep])
    marks = ink & ~np.isin(labels, list(keep))

    paper = np.median(rgba[..., :3][opaque & ~ink], axis=0).astype(np.uint8)
    grown = np.array(Image.fromarray((marks * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))) > 0
    grown &= opaque & ~np.isin(labels, list(keep))
    rgba[..., :3][grown] = paper
    blurred = np.array(Image.fromarray(rgba[..., :3]).filter(ImageFilter.GaussianBlur(3)))
    edge = np.array(Image.fromarray((grown * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
    edge &= opaque & ~ink
    rgba[..., :3][edge] = blurred[edge]
    Image.fromarray(rgba).save(args.sprite)
    print(f"cleared {count - len(keep)} strokes ({int(marks.sum())} px), paper {tuple(int(c) for c in paper)}")


if __name__ == "__main__":
    main()
