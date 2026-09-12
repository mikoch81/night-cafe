#!/usr/bin/env python3
"""Generate the Night Café LCD art: SVG sources and the PNGs Unity imports.

Everything the amber screen shows is built here from parameters, so a style change is a
diff, not a redraw. The palette comes from Assets/Settings/PaletteConfig.asset - the same
single source the scene setup reads.

    tools/gen_art.py sheet     # docs/art-direction/sheet.png: style variants side by side
    tools/gen_art.py sprites   # Assets/Art/sprites/*.png (+ SVG in Assets/Art/Source/sprites)

Rendering goes through Inkscape when it is installed (full SVG 1.1 filters) and falls back
to rsvg-convert, which handles everything used here. PNGs are exported at 4x the SVG
viewBox, the scale the Unity importers (100 PPU) expect. Standard library only.
"""

import argparse
import os
import re
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PALETTE_ASSET = os.path.join(ROOT, "Assets", "Settings", "PaletteConfig.asset")
SPRITE_PNG_DIR = os.path.join(ROOT, "Assets", "Art", "sprites")
SPRITE_SVG_DIR = os.path.join(ROOT, "Assets", "Art", "Source", "sprites")
SHEET_DIR = os.path.join(ROOT, "docs", "art-direction")

SCALE = 4  # PNG pixels per SVG user unit


# ---------------------------------------------------------------- palette

def load_palette():
    """Reads the Unity colour fields out of PaletteConfig.asset as #rrggbb strings."""
    palette = {}
    with open(PALETTE_ASSET, encoding="utf-8") as f:
        for line in f:
            m = re.match(r"\s*(\w+): \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: [\d.]+\}", line)
            if m:
                r, g, b = (round(float(m.group(i)) * 255) for i in (2, 3, 4))
                palette[m.group(1)] = f"#{r:02x}{g:02x}{b:02x}"
    for key in ("activeAmber", "brightAmber", "glassBlack"):
        if key not in palette:
            sys.exit(f"PaletteConfig.asset has no {key}")
    return palette


# ---------------------------------------------------------------- svg helpers

class Style:
    """Rendering parameters shared by every sprite of one art-direction variant."""

    def __init__(self, key, name, gap, glow, cutouts, details=False):
        self.key = key            # short id used in file names
        self.name = name          # label on the sheet
        self.gap = gap            # width of the dark seam between LCD segments (0 = none)
        self.glow = glow          # feGaussianBlur stdDeviation of the burnt-in glow
        self.cutouts = cutouts    # draw inner details as dark cut-outs
        self.details = details    # storytelling extras: steam, mop strands, sweat drop


def svg(width, height, body, details, style, palette):
    """Wraps segment shapes in the LCD look: a blurred amber glow under crisp segments.

    `body` is the amber-filled geometry, `details` the dark seams and cut-outs. Details are
    clipped to the silhouette and drawn above the glow layer, so the glow stays continuous
    like real backlight bleed - a groove between segments never darkens the halo around them.
    """
    amber = palette["activeAmber"]
    return f"""<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 {width} {height}">
<defs>
  <filter id="glow" x="-40%" y="-40%" width="180%" height="180%">
    <feGaussianBlur stdDeviation="{style.glow}"/>
  </filter>
  <g id="seg" fill="{amber}" stroke="none">
{body}
  </g>
  <mask id="ink" maskUnits="userSpaceOnUse" x="0" y="0" width="{width}" height="{height}">
    <g fill="#fff" stroke="none">
{body.replace(amber, "#fff")}
    </g>
  </mask>
</defs>
<use xlink:href="#seg" filter="url(#glow)" opacity="0.85"/>
<use xlink:href="#seg"/>
<g mask="url(#ink)">
{details}
</g>
</svg>
"""


def seam(points, style, palette, width=None):
    """Dark groove between two LCD segments; a no-op for styles without gaps."""
    if style.gap <= 0:
        return ""
    w = width or style.gap
    d = "M" + " L".join(f"{x} {y}" for x, y in points)
    return (f'    <path d="{d}" fill="none" stroke="{palette["glassBlack"]}" '
            f'stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round" class="seam"/>\n')


def cut(shape, style, palette):
    """Inner detail rendered as a dark cut-out (eye, apron pocket) when the style wants it."""
    if not style.cutouts:
        return ""
    return shape.replace("/>", f' fill="{palette["glassBlack"]}"/>\n')


def cut_stroke(d, width, style, palette, extra=""):
    """Inner detail rendered as a dark line (apron outline, pocket) when the style wants it."""
    if not style.cutouts:
        return ""
    return stroke_path(d, width, palette["glassBlack"], extra)


def rrect(x, y, w, h, r, extra=""):
    return f'    <rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" {extra}/>\n'


def circle(cx, cy, r, extra=""):
    return f'    <circle cx="{cx}" cy="{cy}" r="{r}" {extra}/>\n'


def ellipse(cx, cy, rx, ry, extra=""):
    return f'    <ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" {extra}/>\n'


def path(d, extra=""):
    return f'    <path d="{d}" {extra}/>\n'


def stroke_path(d, width, colour, extra=""):
    return (f'    <path d="{d}" fill="none" stroke="{colour}" stroke-width="{width}" '
            f'stroke-linecap="round" stroke-linejoin="round" {extra}/>\n')


# ---------------------------------------------------------------- barista "Miro"
# Canvas 110x100. Shared anchor for every pose (SpriteAnchors.Barista): x 47.9, feet y 89.
# The barista faces right; the game flips him for the left lanes.

# Near-arm angle (degrees, negative = raised), saucer offset along the arm and whether the
# saucer carries a cup. The catch pose puts the saucer where the rails end (K5 sits at about
# (93, 37) in this canvas), up/down lift or drop it towards the lane the barista serves.
BARISTA_POSES = {
    "up": dict(arm=-26, saucer=True, cup=False, far=8, miss=False),
    "down": dict(arm=2, saucer=True, cup=False, far=8, miss=False),
    "catch": dict(arm=-12, saucer=True, cup=True, far=8, miss=False),
    "miss": dict(arm=42, saucer=False, cup=False, far=-14, miss=True),
    # Breather (GDD 2.6): the barista wipes his hands on the apron; alternated with "down".
    "wipe": dict(arm=58, saucer=False, cup=False, far=8, miss=False, cloth=True),
}


def barista(pose, style, palette):
    """Segmented LCD barista in one of BARISTA_POSES. Same canvas and anchor for every pose."""
    import math
    amber = palette["activeAmber"]
    bright = palette["brightAmber"]
    p = BARISTA_POSES[pose]
    b, d = [], []
    sx, sy = 57, 44                                   # near shoulder joint
    theta = math.radians(p["arm"])
    cos, sin = math.cos(theta), math.sin(theta)

    head_tilt = 'transform="rotate(9 42 34)"' if p["miss"] else ""
    b.append(f'    <g {head_tilt}>\n')
    b.append(path("M28 16 Q41 -2 56 14 L56 16.5 L28 16.5 Z"))              # cap crown
    b.append(rrect(50, 13.5, 17, 4, 2))                                     # visor
    b.append(rrect(30, 17.5, 24, 17, 9))                                    # head
    b.append("    </g>\n")
    if p["miss"]:
        d.append(cut_stroke("M47 23 l3.4 3.4 M50.4 23 l-3.4 3.4", 1.4, style, palette))  # x-eye
        if style.details:
            b.append(path("M60 24 q2.6 3.4 0 5.6 q-2.6 -2.2 0 -5.6 z"))     # sweat drop
    else:
        d.append(cut(circle(48.5, 25, 1.7), style, palette))                # eye
    b.append(rrect(38, 33, 8, 7, 2))                                        # neck
    b.append(rrect(28, 39, 30, 12, 5))                                      # chest
    b.append(path("M31 50 L55 50 L53 71 Q43 74 33 71 Z"))                   # apron
    b.append(rrect(21, 43, 7, 22, 3.5, f'transform="rotate({p["far"]} 24.5 43)"'))  # far arm
    b.append(rrect(sx, sy - 3.5, 30, 7, 3.5, f'transform="rotate({p["arm"]} {sx} {sy})"'))  # near arm
    if p["saucer"]:
        cx, cy = sx + 37 * cos, sy + 37 * sin - 2
        b.append(ellipse(round(cx, 1), round(cy, 1), 13, 3.4))              # saucer
        d.append(seam([(round(sx + 28 * cos - 4 * sin, 1), round(sy + 28 * sin + 4 * cos, 1)),
                       (round(sx + 28 * cos + 4 * sin, 1), round(sy + 28 * sin - 4 * cos, 1))],
                      style, palette))                                      # arm / saucer
        if p["cup"]:
            b.append(rrect(round(cx - 8, 1), round(cy - 12.5, 1), 15, 11, 2, f'fill="{bright}"'))  # cup
            b.append(path(f"M{cx + 7:.1f} {cy - 10:.1f} h3.5 a3 3 0 0 1 0 6 h-3.5 v-2 h3.5 "
                          f"a1 1 0 0 0 0 -2 h-3.5 z", f'fill="{bright}"'))  # cup handle
            d.append(seam([(round(cx - 9, 1), round(cy - 1.2, 1)), (round(cx + 9, 1), round(cy - 1.2, 1))],
                          style, palette))                                  # cup / saucer
            if style.details:
                b.append(stroke_path(f"M{cx - 4:.1f} {cy - 15:.1f} q1.5 -2.5 0 -5 M{cx + 1:.1f} {cy - 15:.1f} q1.5 -2.5 0 -5",
                                     1.3, bright, 'opacity="0.75"'))        # steam
    if p.get("cloth"):
        b.append(path("M60 58 q5 -4 9 0 q3 5 -1 9 q-5 3 -9 -1 q-3 -4 1 -8 z"))  # cloth in the near hand
        d.append(seam([(58, 60), (62, 56)], style, palette))                 # hand / cloth
    b.append(rrect(32, 72, 9, 15, 3))                                       # leg
    b.append(rrect(45, 72, 9, 15, 3))                                       # leg
    b.append(rrect(31, 86, 12, 4, 2))                                       # shoe
    b.append(rrect(44, 86, 13, 4, 2))                                       # shoe

    # grooves between the segments
    d.append(seam([(27, 16.8), (68, 16.8)], style, palette))                # cap / head
    d.append(seam([(29, 33.5), (55, 33.5)], style, palette))                # head / neck
    d.append(seam([(27, 39.5), (59, 39.5)], style, palette))                # neck / chest
    d.append(seam([(29, 50), (57, 50)], style, palette))                    # chest / apron
    d.append(seam([(round(sx + 1.5 * cos - 5 * sin, 1), round(sy + 1.5 * sin + 5 * cos, 1)),
                   (round(sx + 1.5 * cos + 5 * sin, 1), round(sy + 1.5 * sin - 5 * cos, 1))],
                  style, palette))                                          # chest / near arm
    d.append(seam([(21.5, 44), (28.5, 44)], style, palette))                # chest / far arm
    d.append(seam([(43, 71), (43, 87)], style, palette))                    # between legs
    d.append(seam([(30, 71.5), (56, 71.5)], style, palette))                # apron / legs
    d.append(seam([(30, 86), (58, 86)], style, palette))                    # legs / shoes

    return svg(110, 100, "".join(b), "".join(d), style, palette)


def barista_up(style, palette):
    """Pose 'up': saucer raised towards the upper lane."""
    amber = palette["activeAmber"]
    b, d = [], []

    if style.key in ("seg", "lcd"):
        return barista("up", style, palette)

    elif style.key == "bold":
        # B - Game & Watch: one heavy silhouette, details as cut-outs.
        b.append(path("M25 18 Q41 -3 57 16 L57 19 L25 19 Z"))               # cap crown
        b.append(rrect(48, 15, 21, 5, 2.5))                                 # visor
        b.append(rrect(27, 19, 28, 20, 11))                                 # head
        d.append(cut(circle(49, 28, 2.2), style, palette))                  # eye
        b.append(rrect(26, 38, 34, 36, 7))                                  # torso
        d.append(cut_stroke("M31 50 L55 50 L53 71 Q43 73 33 71 Z", 1.6, style, palette))  # apron outline
        b.append(rrect(18, 41, 9, 24, 4.5, 'transform="rotate(10 22.5 41)"'))  # far arm
        b.append(rrect(57, 39, 34, 9, 4.5, 'transform="rotate(-32 57 43.5)"'))  # near arm
        b.append(ellipse(92, 19, 14, 4))                                    # saucer
        b.append(rrect(30, 72, 11, 15, 3.5))                                # leg
        b.append(rrect(45, 72, 11, 15, 3.5))                                # leg
        b.append(rrect(28, 85, 14, 5, 2.5))                                 # shoe
        b.append(rrect(44, 85, 15, 5, 2.5))                                 # shoe

    else:
        # C - fine line: slimmer figure with more storytelling detail.
        b.append(path("M30 15 Q41 1 54 13.5 L54 15.5 L30 15.5 Z"))          # cap crown
        b.append(rrect(50, 13, 16, 3.5, 1.75))                              # visor
        b.append(rrect(32, 16.5, 21, 16, 8))                                # head
        d.append(cut(circle(48, 23.5, 1.5), style, palette))                # eye
        d.append(cut(path("M44 28.5 Q48.5 26.5 53 28.5 Q48.5 31.5 44 28.5 Z"), style, palette))  # moustache
        b.append(rrect(39, 32, 6, 6, 1.5))                                  # neck
        d.append(cut(path("M38.5 38 L42 40.5 L45.5 38 L42 35.5 Z"), style, palette))  # bow tie
        b.append(rrect(30, 37.5, 25, 34, 5))                                # torso
        d.append(cut_stroke("M33 48 L52 48 L50.5 69 Q42.5 71 34.5 69 Z", 1.2, style, palette))  # apron outline
        d.append(cut_stroke("M38 56 h9 v6 h-9 z", 1.1, style, palette))    # pocket
        b.append(rrect(23.5, 40, 6, 21, 3, 'transform="rotate(9 26.5 40)"'))   # far arm
        b.append(rrect(54, 40, 32, 5.5, 2.75, 'transform="rotate(-32 54 42.75)"'))  # near arm
        d.append(cut(rrect(62.5, 38.4, 2, 7, 0.5, 'transform="rotate(-32 54 42.75)"'), style, palette))  # rolled sleeve
        b.append(ellipse(90, 21, 12, 3))                                    # saucer
        b.append(rrect(84, 12.5, 10, 8, 1.5))                               # cup on the saucer
        b.append(path("M94 15 h3.5 a2.8 2.8 0 0 1 0 5.6 h-3.5 v-2 h3.5 a0.8 0.8 0 0 0 0 -1.6 h-3.5 z"))  # cup handle
        b.append(stroke_path("M87 10 q1.5 -2.5 0 -5 M91 10 q1.5 -2.5 0 -5", 1.2, amber, 'opacity="0.7"'))  # steam
        b.append(rrect(33, 71, 7, 16, 2.5))                                 # leg
        b.append(rrect(45, 71, 7, 16, 2.5))                                 # leg
        b.append(rrect(31, 86.5, 11, 3.5, 1.75))                            # shoe
        b.append(rrect(44, 86.5, 12, 3.5, 1.75))                            # shoe

    return svg(110, 100, "".join(b), "".join(d), style, palette)


# ---------------------------------------------------------------- cat "Sablé"
# Canvas 100x56. Anchor (SpriteAnchors.Cat): x 34, bottom y 53.5. The cat sits facing right
# and pushes a mop; frame b swings the mop.

def cat(frame, style, palette):
    """Segmented LCD cat, sitting, pushing a mop. Frame b swings the mop to the right."""
    amber = palette["activeAmber"]
    b, d = [], []
    swing = frame == "b"

    b.append(path("M22 53 C12 53 7 42 13 32 C21 23 37 19 46 22 C52 25 54 40 54 53 Z"))  # body
    b.append(rrect(46, 33, 8, 20, 3.5))                                      # near front leg
    if swing:
        b.append(rrect(52, 33, 24, 5.5, 2.75, 'transform="rotate(-14 52 35.5)"'))  # paw pushing
        b.append(rrect(72, 2, 4, 42, 2, 'transform="rotate(-16 74 8)"'))     # handle, tilted
        b.append(ellipse(87, 49, 12, 4.5))                                   # mop head, swung
        d.append(seam([(79, 45.5), (95, 45.5)], style, palette))             # handle / mop
        d.append(seam([(53.5, 31), (53.5, 39)], style, palette))             # leg / paw
    else:
        b.append(rrect(52, 35, 21, 5.5, 2.75, 'transform="rotate(-6 52 37.5)"'))  # paw on the handle
        b.append(rrect(72.5, 4, 4, 41, 2))                                   # mop handle
        b.append(ellipse(75, 49, 12, 4.5))                                   # mop head
        d.append(seam([(68, 45), (82, 45)], style, palette))                 # handle / mop
        d.append(seam([(53.5, 33), (53.5, 41)], style, palette))             # leg / paw
    b.append(circle(54, 17, 10))                                             # head
    b.append(path("M46 10 L44 -0.5 L52 7 Z"))                                # ear
    b.append(path("M57 7.5 L63 -1 L64 10 Z"))                                # ear
    d.append(cut(circle(57.5, 15.5, 1.6), style, palette))                   # eye
    b.append(stroke_path("M14 50 Q2 45 5 33 Q6 28 11 29", 4.8, amber))       # tail
    if style.details:
        b.append(stroke_path("M61 20 l5.5 -1 M61 21.5 l5.5 1.2", 0.9, amber))  # whiskers
        hx = 87 if swing else 75
        b.append(stroke_path(f"M{hx - 9} 50.5 l-1 4.5 M{hx - 4.5} 51.5 l-0.5 4 M{hx} 51.5 l0 4.5 "
                             f"M{hx + 4.5} 51.5 l0.5 4 M{hx + 9} 50.5 l1 4.5", 1.6, amber))  # strands

    d.append(seam([(45, 24), (52, 27)], style, palette))                     # body / head
    d.append(seam([(45.5, 32), (45.5, 53)], style, palette))                 # body / near leg
    d.append(seam([(13, 45), (17, 50)], style, palette))                     # body / tail
    d.append(seam([(45.5, 10.5), (52.5, 8)], style, palette))                # head / ears
    d.append(seam([(56.5, 8.5), (64.5, 10.5)], style, palette))

    return svg(100, 56, "".join(b), "".join(d), style, palette)


def cat_a(style, palette):
    """Frame a: sitting upright, one paw on the mop handle, mop resting on the floor."""
    amber = palette["activeAmber"]
    bold = style.key == "bold"
    fine = style.key == "fine"
    b, d = [], []

    # One sitting profile for every style: haunch at the left, back rising to the shoulders,
    # chest dropping straight to the front paws. Bold pads it, fine trims it.
    pad = 1.5 if bold else (-0.5 if fine else 0)
    b.append(path(f"M22 53 C{12 - pad} 53 {7 - pad} 42 {13 - pad} {32 - pad} "
                  f"C21 {23 - pad} 37 {19 - pad} 46 {22 - pad} "
                  f"C{52 + pad} 25 {54 + pad} 40 {54 + pad} 53 Z"))
    b.append(rrect(46, 33, 8 + pad, 20, 3.5))                                # near front leg
    b.append(rrect(52, 35, 21, 5.5 + pad, 2.75, 'transform="rotate(-6 52 37.5)"'))  # paw on the handle
    b.append(circle(54, 17, 10 + pad))                                       # head
    b.append(path("M46 10 L44 -0.5 L52 7 Z"))                                # ear
    b.append(path("M57 7.5 L63 -1 L64 10 Z"))                                # ear
    d.append(cut(circle(57.5, 15.5, 2.2 if bold else 1.6), style, palette))  # eye
    b.append(stroke_path("M14 50 Q2 45 5 33 Q6 28 11 29", 6 if bold else (4 if fine else 4.8), amber))  # tail
    b.append(rrect(72.5, 4, 4.5 if bold else (3.5 if fine else 4), 41, 2))   # mop handle
    b.append(ellipse(75, 49, 12, 4.5))                                       # mop head

    if style.key in ("seg", "lcd"):
        return cat("a", style, palette)

    if False:
        d.append(seam([(45, 24), (52, 27)], style, palette))                 # body / head
        d.append(seam([(45.5, 32), (45.5, 53)], style, palette))             # body / near leg
        d.append(seam([(53.5, 33), (53.5, 41)], style, palette))             # leg / paw
        d.append(seam([(13, 45), (17, 50)], style, palette))                 # body / tail
        d.append(seam([(68, 45), (82, 45)], style, palette))                 # handle / mop
        d.append(seam([(45.5, 10.5), (52.5, 8)], style, palette))            # head / ears
        d.append(seam([(56.5, 8.5), (64.5, 10.5)], style, palette))
    elif fine:
        d.append(cut(path("M62 19.5 L64.3 18.5 L63.8 21 Z"), style, palette))  # nose
        b.append(stroke_path("M61 22 l6 -1 M61 23.5 l6 1.5", 0.9, amber))    # whiskers
        d.append(cut_stroke("M4 39 l5 1 M3.5 43 l5 0.5", 1.1, style, palette))  # tail stripes
        d.append(cut(rrect(45, 24.5, 9, 2.2, 1.1, 'transform="rotate(-20 49.5 25.5)"'), style, palette))  # collar
        b.append(stroke_path("M66 50 l-1 5 M70 51 l-0.5 5 M74.5 51 l0 5 M79 51 l0.5 5 M83 50 l1 5", 1.6, amber))  # strands

    return svg(100, 56, "".join(b), "".join(d), style, palette)


# ---------------------------------------------------------------- props

def cup(style, palette):
    """Canvas 52x34, pivot (26, 13.5). Mode B tints the whole sprite, so it is one colour."""
    bright = palette["brightAmber"]
    b, d = [], []
    b.append(rrect(3, 6, 29, 21, 4, f'fill="{bright}"'))                         # body
    b.append(path("M32 10 h8 a7.5 7.5 0 0 1 0 15 h-8 v-4 h8 a3.5 3.5 0 0 0 0 -7 h-8 z", f'fill="{bright}"'))  # handle
    d.append(seam([(32.5, 8), (32.5, 27)], style, palette))                      # body / handle
    if style.details:
        b.append(stroke_path("M11 4 q2 -2.5 0 -5 M18 4 q2 -2.5 0 -5", 1.6, bright, 'opacity="0.75"'))  # steam
    return svg(52, 34, "".join(b), "".join(d), style, palette)


def cup_broken(style, palette):
    """Canvas 52x40, pivot (26, 19.6): two shards, a spill and flying drops."""
    bright = palette["brightAmber"]
    b, d = [], []
    b.append(path("M5 27 L9 10 L20 6 L23 17 L17 27 Z", f'fill="{bright}"'))       # left shard
    b.append(path("M25 24 L27 8 L38 12 L43 24 L36 30 Z", f'fill="{bright}"'))     # right shard
    b.append(path("M4 33 Q24 39 44 33 Q46 36 44 37 Q24 41 4 37 Q2 35 4 33 Z", f'fill="{bright}"'))  # spill
    b.append(circle(45, 6, 2.4, f'fill="{bright}"'))                             # drops
    b.append(circle(48, 15, 1.9, f'fill="{bright}"'))
    b.append(circle(8, 4, 1.7, f'fill="{bright}"'))
    d.append(seam([(11, 12), (18, 24)], style, palette, 1.2))                    # crack
    d.append(seam([(30, 12), (34, 27)], style, palette, 1.2))
    return svg(52, 40, "".join(b), "".join(d), style, palette)


def stain(style, palette):
    """Canvas 60x34, pivot (26.25, 20.6). Puddle with a glossy cut-out."""
    b, d = [], []
    b.append(path("M2 18 q8 -14 19 -6 q13 -10 21 2 q13 0 8 11 q3 9 -11 9 l-26 0 q-15 0 -11 -16z"))
    d.append(cut(ellipse(20, 16, 5, 2.2), style, palette))                       # gloss
    return svg(60, 34, "".join(b), "".join(d), style, palette)


def machine_head(style, palette):
    """Canvas 46x42, pivot (22, 19): espresso group head with a portafilter and a spout."""
    b, d = [], []
    b.append(rrect(2, 2, 40, 22, 5))                                             # group body
    b.append(rrect(8, 25, 28, 6, 2))                                             # portafilter
    b.append(rrect(18, 32, 8, 8, 2))                                             # spout
    b.append(rrect(36, 8, 8, 5, 2.5))                                            # portafilter handle
    d.append(cut(circle(11, 13, 3.2), style, palette))                           # ready light
    d.append(seam([(2, 24.5), (42, 24.5)], style, palette))                      # body / portafilter
    d.append(seam([(14, 31.5), (30, 31.5)], style, palette))                     # portafilter / spout
    d.append(seam([(37, 6), (37, 15)], style, palette))                          # body / handle
    return svg(46, 42, "".join(b), "".join(d), style, palette)


def order_panel(style, palette):
    """Canvas 130x52. Frame plus a white window at (14..50, 12..40) - OrderPanelView lays the
    colour swatch quad over that exact rectangle, so it must not move."""
    amber = palette["activeAmber"]
    b, d = [], []
    b.append(stroke_path("M12 2 h108 a10 10 0 0 1 10 10 v28 a10 10 0 0 1 -10 10 h-108 a10 10 0 0 1 -10 -10 v-28 a10 10 0 0 1 10 -10 z", 3, amber))
    b.append(rrect(14, 12, 36, 28, 5, 'fill="#ffffff"'))                        # swatch window
    b.append(path("M50 20 h5 a5 5 0 0 1 0 10 h-5 v-3 h5 a2 2 0 0 0 0 -4 h-5 z"))  # window's cup handle
    return svg(130, 52, "".join(b), "".join(d), style, palette)


# ---------------------------------------------------------------- neon cat (rollover 999)
# Canvas 160x80, centred pivot. Six cumulative frames: the city neons "arrange themselves"
# into a cat over two seconds (GDD 2.7), one stroke group per frame.

NEON_STROKES = [
    "M22 62 Q10 44 24 30 Q34 20 52 24",                       # back
    "M52 24 L58 10 L72 22 L96 20 L110 8 L116 26",             # ears and head top
    "M116 26 Q124 40 112 48 Q98 54 86 48",                    # face and chin
    "M86 48 Q84 60 92 66 L22 66 Q16 64 22 62",                # chest and floor line
    "M22 62 Q4 58 6 44 Q8 34 18 36",                          # tail
    "M68 32 l4 0 M92 32 l4 0 M80 40 l0 3 M74 46 q6 4 12 0",   # eyes, nose, mouth
]


def neon_cat(frame, style, palette):
    bright = palette["brightAmber"]
    b = []
    for i in range(frame):
        b.append(stroke_path(NEON_STROKES[i], 3.2, bright))
    if frame > 0 and style.details:
        # the newest stroke still "flickers on": a thinner echo just outside it
        b.append(stroke_path(NEON_STROKES[frame - 1], 6, bright, 'opacity="0.35"'))
    return svg(160, 80, "".join(b), "", style, palette)


# Final style: variant A (segmented LCD) plus the few storytelling details from C that survive
# the phone scale - chosen by Michał on 2026-09-13 (docs/art-direction.md).
# gap 2.2 (not the sheet's 1.5): the barista is ~100 px tall on a phone, a groove must survive that.
FINAL = Style("lcd", "Neo-LCD", gap=2.2, glow=2.4, cutouts=True, details=True)

SPRITES = {
    "barista_up": (lambda st, pal: barista("up", st, pal), 110, 100),
    "barista_down": (lambda st, pal: barista("down", st, pal), 110, 100),
    "barista_catch": (lambda st, pal: barista("catch", st, pal), 110, 100),
    "barista_miss": (lambda st, pal: barista("miss", st, pal), 110, 100),
    "barista_wipe": (lambda st, pal: barista("wipe", st, pal), 110, 100),
    "cat_a": (lambda st, pal: cat("a", st, pal), 100, 56),
    "cat_b": (lambda st, pal: cat("b", st, pal), 100, 56),
    "cup": (cup, 52, 34),
    "cup_broken": (cup_broken, 52, 40),
    "stain": (stain, 60, 34),
    "machine_head": (machine_head, 46, 42),
    "order_panel": (order_panel, 130, 52),
}
for _i in range(1, 7):
    SPRITES[f"neon_cat_{_i}"] = ((lambda n: lambda st, pal: neon_cat(n, st, pal))(_i), 160, 80)


STYLES = [
    Style("seg", "A  segmentowy LCD", gap=1.5, glow=2.4, cutouts=True),
    Style("bold", "B  Game & Watch", gap=0, glow=2.6, cutouts=True),
    Style("fine", "C  kreskowy", gap=0, glow=2.0, cutouts=True),
]

SHEET_SPRITES = [("barista_up", barista_up, 110, 100), ("cat_a", cat_a, 100, 56)]


# ---------------------------------------------------------------- rendering

def renderer():
    if shutil.which("inkscape"):
        return "inkscape"
    if shutil.which("rsvg-convert"):
        return "rsvg-convert"
    sys.exit("need inkscape or rsvg-convert")


def render(svg_path, png_path, width, height, tool):
    os.makedirs(os.path.dirname(png_path), exist_ok=True)
    if tool == "inkscape":
        cmd = ["inkscape", svg_path, "--export-type=png", f"--export-filename={png_path}",
               f"--export-width={width * SCALE}", f"--export-height={height * SCALE}",
               "--export-background-opacity=0"]
    else:
        cmd = ["rsvg-convert", "-w", str(width * SCALE), "-h", str(height * SCALE),
               "-o", png_path, svg_path]
    subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)


# ---------------------------------------------------------------- commands

def cmd_sheet(args):
    """Renders every style variant of the sheet sprites and tiles them for review."""
    palette = load_palette()
    tool = renderer()
    cells = []  # (style label, sprite name, png)
    for style in STYLES:
        for name, builder, w, h in SHEET_SPRITES:
            svg_path = os.path.join(SHEET_DIR, "svg", f"{name}_{style.key}.svg")
            png_path = os.path.join(SHEET_DIR, "png", f"{name}_{style.key}.png")
            write(svg_path, builder(style, palette))
            render(svg_path, png_path, w, h, tool)
            cells.append((style, name, png_path))

    current = [os.path.join(SPRITE_PNG_DIR, f"{name}.png") for name, *_ in SHEET_SPRITES]
    sheet = os.path.join(SHEET_DIR, "sheet.png")
    compose_sheet(cells, current, sheet, palette)
    print(f"wrote {os.path.relpath(sheet, ROOT)} (renderer: {tool})")


def compose_sheet(cells, current, out, palette):
    """ImageMagick montage: one column per style (plus the current art), one row per sprite,
    and a half-scale row so the silhouettes can be judged at phone size."""
    glass = palette["glassBlack"]
    amber = palette["activeAmber"]
    columns = [("obecnie (v1)", current)]
    for style in STYLES:
        columns.append((style.name, [p for s, _, p in cells if s is style]))

    tmp = os.path.join(SHEET_DIR, "png", "_col")
    os.makedirs(tmp, exist_ok=True)
    col_pngs = []
    for c, (label, pngs) in enumerate(columns):
        col = os.path.join(tmp, f"col{c}.png")
        cmd = ["magick", "-background", glass, "-fill", amber, "-font", "Liberation-Mono-Bold",
               "-pointsize", "30", f"label:{label}", "-gravity", "center"]
        for p in pngs:
            cmd += ["(", p, "-gravity", "center", "-background", glass, "-extent", "480x420", ")"]
        cmd += ["("]
        for p in pngs:
            cmd += ["(", p, "-resize", "45%", ")"]
        cmd += ["-background", glass, "+append", "-extent", "480x220", ")"]
        cmd += ["-append", "-bordercolor", glass, "-border", "12", col]
        subprocess.run(cmd, check=True)
        col_pngs.append(col)
    subprocess.run(["magick", *col_pngs, "-background", glass, "+append", out], check=True)
    shutil.rmtree(tmp)


def cmd_sprites(args):
    """Writes every game sprite (SVG source + 4x PNG) in the final style."""
    palette = load_palette()
    tool = renderer()
    names = args.only or sorted(SPRITES)
    for name in names:
        builder, w, h = SPRITES[name]
        svg_path = os.path.join(SPRITE_SVG_DIR, f"{name}.svg")
        png_path = os.path.join(SPRITE_PNG_DIR, f"{name}.png")
        write(svg_path, builder(FINAL, palette))
        render(svg_path, png_path, w, h, tool)
        print(f"{name}.png {w * SCALE}x{h * SCALE}")
    print(f"renderer: {tool}")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="cmd", required=True)
    sub.add_parser("sheet", help="render the art-direction comparison sheet").set_defaults(fn=cmd_sheet)
    sprites = sub.add_parser("sprites", help="render the game sprites in the final style")
    sprites.add_argument("only", nargs="*", help="sprite names (default: all)")
    sprites.set_defaults(fn=cmd_sprites)
    args = parser.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
