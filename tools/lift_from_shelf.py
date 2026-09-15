#!/usr/bin/env python3
"""Lifts dark figures off a painted shelf in a Midjourney sheet, so cut_sheet.py can cut them.

    python3 tools/lift_from_shelf.py art/midjourney/19_sable_sleep.png art/midjourney/19_sable_sleep_clean.png \
        --box 770 820 1400 1160 --box 1800 820 2460 1160 --ledge 1085

Everything outside the boxes is painted white. Inside a box the figure is whatever is not the
shelf: warm wood tones (hue 15-50 deg, saturated) are painted white, and the box is narrowed
to the figure's own width measured above `--ledge` (the row where the shelf's top edge starts),
so the edge line does not ride along on either side. Pillow + numpy.
"""
import argparse

import numpy as np
from PIL import Image


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sheet")
    ap.add_argument("out")
    ap.add_argument("--box", type=int, nargs=4, action="append", required=True, metavar=("X1", "Y1", "X2", "Y2"))
    ap.add_argument("--ledge", type=int, required=True, help="row from which the shelf begins (its top edge line)")
    ap.add_argument("--dark", type=int, default=90, help="max channel value that still counts as the figure's ink")
    args = ap.parse_args()

    rgb = np.array(Image.open(args.sheet).convert("RGB"))
    hsv = np.array(Image.fromarray(rgb).convert("HSV")).astype(int)
    h, w, _ = rgb.shape
    out = np.full_like(rgb, 255)

    wood = (hsv[..., 0] >= 10) & (hsv[..., 0] <= 36) & (hsv[..., 1] > 60) & (hsv[..., 2] > 60)  # PIL hue is 0-255
    dark = rgb.max(axis=2) < args.dark

    for x1, y1, x2, y2 in args.box:
        figure = dark[y1:args.ledge, x1:x2]
        cols = np.where(figure.any(axis=0))[0]
        if len(cols) == 0:
            raise SystemExit(f"no figure above the ledge in box {x1, y1, x2, y2}")
        fx1, fx2 = x1 + cols.min(), x1 + cols.max() + 1
        keep = ~wood[y1:y2, fx1:fx2]
        out[y1:y2, fx1:fx2][keep] = rgb[y1:y2, fx1:fx2][keep]
        print(f"box {x1},{y1}-{x2},{y2}: figure columns {fx1}-{fx2}")

    Image.fromarray(out).save(args.out)


if __name__ == "__main__":
    main()
