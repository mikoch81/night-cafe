#!/usr/bin/env python3
"""Cuts a Midjourney sheet on a plain background into transparent PNG sprites.

    python3 tools/cut_sheet.py art/midjourney/13_miro_sheet.png Assets/Art/screen_v3 --prefix miro \
        --names up down catch miss wipe --floor 1265
    python3 tools/cut_sheet.py art/midjourney/14_sable_sheet.png Assets/Art/screen_v3 --prefix sable \
        --names skip a b --split 540 1078
    python3 tools/cut_sheet.py art/midjourney/15_machine.png Assets/Art/screen_v3 --prefix machine --names head --fuzz 5

Steps: the ground scribble below `--floor` is painted over with the background colour, the
background is flood-filled to transparency from the corners and edges (pockets enclosed by
outlines, like the white between two legs, open onto the floor and drain too), opaque blobs are
found with ImageMagick's connected components, grouped into poses (the N biggest blobs are the
bodies; a loose blob - a cup in the air, a hand cut off by a thin wrist - joins the body whose
x-range it overlaps or is nearest to; `--split` instead assigns every blob by the x of its
centre), and each pose is masked to its own blobs before being cropped, trimmed and padded, so
a neighbour's tail never rides along. Standard library plus ImageMagick (`magick`).
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
    w, h = run("magick", "identify", "-format", "%w %h", path, capture=True).split()
    return int(w), int(h)


def corner_colour(path):
    return run("magick", path, "-format", "%[pixel:p{0,0}]", "info:", capture=True).strip()


def knock_out(src, dst, fuzz, floor=None, floor_keep=()):
    """`floor_keep` = x ranges (x1, x2, x1, x2, ...) left unpainted below the floor, for a mop
    head or a dropped cup that sits on the ground line."""
    w, h = size_of(src)
    paint = []
    if floor is not None and 0 < floor < h:
        edges = [0] + [int(v) for v in floor_keep] + [w]
        paint = ["-fill", corner_colour(src)]
        for i in range(0, len(edges), 2):
            if edges[i + 1] > edges[i]:
                paint += ["-draw", f"rectangle {edges[i]},{floor} {edges[i + 1]},{h}"]
    seeds = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1),
             (w // 2, 0), (w // 2, h - 1), (0, h // 2), (w - 1, h // 2)]
    draws = []
    for x, y in seeds:
        draws += ["-draw", f"alpha {x},{y} floodfill"]
    run("magick", src, *paint, "-alpha", "set", "-fuzz", f"{fuzz}%", "-fill", "none", *draws,
        # soften the fringe: pixels that are half background lose half their alpha
        "-channel", "A", "-morphology", "Erode", "Diamond:1", "+channel", dst)


def cc_args(min_area):
    return ["-alpha", "extract", "-threshold", "10%",
            "-define", "connected-components:verbose=true",
            "-define", f"connected-components:area-threshold={min_area}"]


def components(path, min_area):
    """[(id, x, y, w, h, area)] of every opaque blob."""
    out = run("magick", path, *cc_args(min_area), "-connected-components", "8", "null:", capture=True)
    blobs = []
    for line in out.splitlines():
        # "  1: 231x412+55+30 170.5,236.0 95000 srgb(255,255,255)"   (big areas print as 1.38e+06)
        m = re.match(r"\s*(\d+):\s+(\d+)x(\d+)\+(\d+)\+(\d+)\s+\S+\s+([\d.e+]+)\s+(\S+)", line)
        if not m:
            continue
        cid, w, h, x, y, area, colour = m.groups()
        if "255" not in colour:
            continue  # the transparent background
        blobs.append((int(cid), int(x), int(y), int(w), int(h), int(float(area))))
    return blobs


def x_distance(a, b):
    """0 when the x-ranges overlap, else the gap between them."""
    return max(0, a[1] - (b[1] + b[3]), b[1] - (a[1] + a[3]))


def centre(b):
    return b[1] + b[3] / 2


def group_auto(blobs, count, gap):
    bodies = sorted(sorted(blobs, key=lambda b: b[5], reverse=True)[:count], key=lambda b: b[1])
    groups = [[b] for b in bodies]
    for b in blobs:
        if b in bodies:
            continue
        nearest = min(range(len(groups)),
                      key=lambda i: (x_distance(groups[i][0], b), abs(centre(groups[i][0]) - centre(b))))
        if x_distance(groups[nearest][0], b) <= gap:
            groups[nearest].append(b)
    return groups


def group_split(blobs, splits, count):
    edges = [-1] + sorted(splits) + [10 ** 9]
    groups = [[] for _ in range(len(edges) - 1)]
    for b in blobs:
        cx = centre(b)
        for i in range(len(edges) - 1):
            if edges[i] < cx <= edges[i + 1]:
                groups[i].append(b)
                break
    if len(groups) != count:
        sys.exit(f"--split gives {len(groups)} poses, --names has {count}")
    return groups


def bbox(group):
    x = min(b[1] for b in group)
    y = min(b[2] for b in group)
    w = max(b[1] + b[3] for b in group) - x
    h = max(b[2] + b[4] for b in group) - y
    return x, y, w, h


def write_pose(cut, group, min_area, pad, out, tmp):
    ids = ",".join(str(b[0]) for b in group)
    x, y, w, h = bbox(group)
    mask = os.path.join(tmp, "mask.png")
    # Kept blobs as a white mask, dilated so the anti-aliased fringe survives the multiply.
    run("magick", cut, *cc_args(min_area), "-define", f"connected-components:keep={ids}",
        "-connected-components", "8", "-threshold", "0", "-morphology", "Dilate", "Diamond:2", "-depth", "8", mask)
    # DstIn: keep the cut's colour, alpha = cut alpha x mask alpha. The crop is a second
    # command on purpose: cropping in the same command as the composite came out as an empty
    # bilevel image in ImageMagick 7.
    masked = os.path.join(tmp, "masked.png")
    run("magick", cut, "(", mask, "-alpha", "copy", ")", "-compose", "DstIn", "-composite", masked)
    run("magick", masked, "-crop", f"{w}x{h}+{x}+{y}", "+repage", "-trim", "+repage",
        "-bordercolor", "none", "-border", str(pad), "PNG32:" + out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sheet")
    ap.add_argument("outdir")
    ap.add_argument("--prefix", required=True)
    ap.add_argument("--names", nargs="+", required=True, help="one per pose, left to right")
    ap.add_argument("--fuzz", type=float, default=10.0, help="background colour tolerance, percent")
    ap.add_argument("--pad", type=int, default=8, help="transparent padding around each sprite")
    ap.add_argument("--gap", type=int, default=120, help="a loose blob this far (px) from a pose's x-range still belongs to it")
    ap.add_argument("--min-area", type=int, default=400, help="ignore specks smaller than this")
    ap.add_argument("--floor", type=int, help="row (px from the top) from which the ground scribble is painted over")
    ap.add_argument("--split", type=int, nargs="*", help="x positions separating the poses (blobs go by their centre)")
    ap.add_argument("--floor-keep", type=int, nargs="*", default=[], help="x1 x2 pairs left unpainted below the floor (a mop head)")
    args = ap.parse_args()

    os.makedirs(args.outdir, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        cut = os.path.join(tmp, "cut.png")
        knock_out(args.sheet, cut, args.fuzz, args.floor, args.floor_keep)
        blobs = components(cut, args.min_area)
        if len(blobs) < len(args.names):
            sys.exit(f"found {len(blobs)} blobs, expected at least {len(args.names)} poses")
        groups = group_split(blobs, args.split, len(args.names)) if args.split else group_auto(blobs, len(args.names), args.gap)
        for group, name in zip(groups, args.names):
            if not group:
                print(f"{name}: no blobs, skipped")
                continue
            out = os.path.join(args.outdir, f"{args.prefix}_{name}.png")
            write_pose(cut, group, args.min_area, args.pad, out, tmp)
            w, h = size_of(out)
            print(f"{out}: {w}x{h} ({len(group)} blobs)")


if __name__ == "__main__":
    main()
