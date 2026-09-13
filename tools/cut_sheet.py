#!/usr/bin/env python3
"""Cuts a Midjourney sheet on a plain background into transparent PNG sprites.

    python3 tools/cut_sheet.py art/midjourney/13_miro_sheet.png Assets/Art/screen_v3 --prefix miro \
        --names up down catch miss wipe
    python3 tools/cut_sheet.py art/midjourney/15_machine.png Assets/Art/screen_v3 --prefix machine --names head

The background colour is sampled at the corners and flood-filled to transparency from every
corner (so enclosed white inside a character, like a cup, survives). The remaining opaque
blobs are found with ImageMagick's connected components, sorted left to right and written as
`<prefix>_<name>.png`, each trimmed and padded. Standard library plus ImageMagick (`magick`).
"""
import argparse
import os
import re
import subprocess
import sys
import tempfile


def run(*cmd, capture=False):
    result = subprocess.run(cmd, check=True, stdout=subprocess.PIPE if capture else None, text=True)
    return result.stdout if capture else None


def size_of(path):
    out = run("magick", "identify", "-format", "%w %h", path, capture=True)
    w, h = out.split()
    return int(w), int(h)


def knock_out(src, dst, fuzz):
    """Background -> alpha 0 by flood-filling from the four corners (and mid-edges, for sheets
    whose blobs touch a corner)."""
    w, h = size_of(src)
    points = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
              (w // 2, 0), (w // 2, h - 1), (0, h // 2), (w - 1, h // 2)]
    draws = []
    for x, y in points:
        draws += ["-draw", f"alpha {x},{y} floodfill"]
    run("magick", src, "-alpha", "set", "-fuzz", f"{fuzz}%", "-fill", "none", *draws,
        # soften the fringe: pixels that are half background lose half their alpha
        "-channel", "A", "-morphology", "Erode", "Diamond:1", "+channel", dst)


def components(path, min_area):
    """(x, y, w, h) of every opaque blob, biggest first, via connected components on the alpha."""
    out = run("magick", path, "-alpha", "extract", "-threshold", "10%",
              "-define", "connected-components:verbose=true",
              "-define", f"connected-components:area-threshold={min_area}",
              "-connected-components", "8", "null:", capture=True)
    boxes = []
    for line in out.splitlines():
        # "  1: 231x412+55+30 170.5,236.0 95000 srgb(255,255,255)"
        m = re.match(r"\s*\d+:\s+(\d+)x(\d+)\+(\d+)\+(\d+)\s+\S+\s+(\d+)\s+(\S+)", line)
        if not m:
            continue
        w, h, x, y, area, colour = m.groups()
        if "255" not in colour and "white" not in colour:
            continue  # background component
        boxes.append((int(x), int(y), int(w), int(h), int(area)))
    return boxes


def merge_overlapping(boxes, gap):
    """Blobs closer than `gap` px horizontally belong to one pose (a dropped cup next to Miro)."""
    boxes = sorted(boxes, key=lambda b: b[0])
    merged = []
    for b in boxes:
        if merged:
            x, y, w, h, a = merged[-1]
            if b[0] <= x + w + gap:
                nx, ny = min(x, b[0]), min(y, b[1])
                nw = max(x + w, b[0] + b[2]) - nx
                nh = max(y + h, b[1] + b[3]) - ny
                merged[-1] = (nx, ny, nw, nh, a + b[4])
                continue
        merged.append(b)
    return merged


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sheet")
    ap.add_argument("outdir")
    ap.add_argument("--prefix", required=True)
    ap.add_argument("--names", nargs="+", required=True, help="one per pose, left to right")
    ap.add_argument("--fuzz", type=float, default=12.0, help="background colour tolerance, percent")
    ap.add_argument("--pad", type=int, default=8, help="transparent padding around each sprite")
    ap.add_argument("--gap", type=int, default=24, help="blobs closer than this (px) are one pose")
    ap.add_argument("--min-area", type=int, default=400, help="ignore specks smaller than this")
    args = ap.parse_args()

    os.makedirs(args.outdir, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        cut = os.path.join(tmp, "cut.png")
        knock_out(args.sheet, cut, args.fuzz)
        boxes = merge_overlapping(components(cut, args.min_area), args.gap)
        boxes = sorted(boxes, key=lambda b: b[4], reverse=True)[:len(args.names)]
        boxes = sorted(boxes, key=lambda b: b[0])
        if len(boxes) != len(args.names):
            sys.exit(f"found {len(boxes)} poses, expected {len(args.names)}: {boxes}")
        for (x, y, w, h, _), name in zip(boxes, args.names):
            out = os.path.join(args.outdir, f"{args.prefix}_{name}.png")
            run("magick", cut, "-crop", f"{w}x{h}+{x}+{y}", "+repage",
                "-bordercolor", "none", "-border", str(args.pad), out)
            print(f"{out}: {w}x{h}")


if __name__ == "__main__":
    main()
