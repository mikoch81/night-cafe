#!/usr/bin/env python3
"""Wipes the painted hands off a clock-face sprite, so the game can draw its own.

    python3 tools/clear_dial.py Assets/Art/screen_v3/clock_face.png

Inside the dial (up to 80 % of the face radius, measured from the sprite's alpha bounds) every
dark stroke that connects to the centre pin is painted over with the face colour; the rim, the
hour dots (which touch nothing) and the pin itself stay. Pillow + numpy.
"""
import argparse

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sprite")
    ap.add_argument("--inner", type=float, default=0.06, help="keep the centre pin inside this fraction of the radius")
    ap.add_argument("--outer", type=float, default=0.8, help="look for hands this far out (short of the rim)")
    ap.add_argument("--dark", type=int, default=150, help="luminance below which a pixel is a hand")
    args = ap.parse_args()

    im = Image.open(args.sprite).convert("RGBA")
    rgba = np.array(im)
    alpha = rgba[..., 3] > 8
    ys, xs = np.where(alpha)
    cx, cy = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
    radius = (xs.max() - xs.min()) / 2
    yy, xx = np.mgrid[:rgba.shape[0], :rgba.shape[1]]
    dist = np.hypot(xx - cx, yy - cy) / radius
    ring = (dist > args.inner) & (dist < args.outer)

    lum = rgba[..., :3].astype(int) @ [299, 587, 114] // 1000
    # the hands are the dark strokes that connect to the centre pin; the hour dots do not
    labels, count = ndimage.label((dist < args.outer) & (lum < args.dark))
    at_centre = set(np.unique(labels[dist < args.inner * 2])) - {0}
    hands = np.isin(labels, list(at_centre)) & ring
    light = ring & ~hands
    fill = np.median(rgba[..., :3][light], axis=0).astype(np.uint8)
    # grow the hand mask a little so the anti-aliased edge of the stroke goes too
    grown = np.array(Image.fromarray((hands * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))) > 0
    grown &= ring
    rgba[..., :3][grown] = fill
    # soften the patch edge so it does not read as a hard cut-out
    blurred = np.array(Image.fromarray(rgba[..., :3]).filter(ImageFilter.GaussianBlur(3)))
    edge = np.array(Image.fromarray((grown * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
    edge &= ring & ~hands
    rgba[..., :3][edge] = blurred[edge]
    Image.fromarray(rgba).save(args.sprite)
    print(f"cleared {int(grown.sum())} px of hands, face colour {tuple(int(c) for c in fill)}")


if __name__ == "__main__":
    main()
