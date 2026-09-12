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

    def __init__(self, key, name, gap, glow, cutouts):
        self.key = key            # short id used in file names
        self.name = name          # label on the sheet
        self.gap = gap            # width of the dark seam between LCD segments (0 = none)
        self.glow = glow          # feGaussianBlur stdDeviation of the burnt-in glow
        self.cutouts = cutouts    # draw inner details as dark cut-outs


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

def barista_up(style, palette):
    """Pose 'up': saucer raised towards the upper lane."""
    amber = palette["activeAmber"]
    b, d = [], []

    if style.key == "seg":
        # A - true LCD: one silhouette split into segments by grooves.
        b.append(path("M28 16 Q41 -2 56 14 L56 16.5 L28 16.5 Z"))          # cap crown
        b.append(rrect(50, 13.5, 17, 4, 2))                                 # visor
        b.append(rrect(30, 17.5, 24, 17, 9))                                # head
        d.append(cut(circle(48.5, 25, 1.7), style, palette))                # eye
        b.append(rrect(38, 33, 8, 7, 2))                                    # neck
        b.append(rrect(28, 39, 30, 12, 5))                                  # chest
        b.append(path("M31 50 L55 50 L53 71 Q43 74 33 71 Z"))               # apron
        b.append(rrect(21, 43, 7, 22, 3.5, 'transform="rotate(8 24.5 43)"'))  # far arm, down
        b.append(rrect(57, 40.5, 33, 7, 3.5, 'transform="rotate(-32 57 44)"'))  # near arm, up
        b.append(ellipse(92, 20.5, 13, 3.4))                                # saucer
        b.append(rrect(32, 72, 9, 15, 3))                                   # leg
        b.append(rrect(45, 72, 9, 15, 3))                                   # leg
        b.append(rrect(31, 86, 12, 4, 2))                                   # shoe
        b.append(rrect(44, 86, 13, 4, 2))                                   # shoe
        # grooves
        d.append(seam([(27, 16.8), (68, 16.8)], style, palette))            # cap / head
        d.append(seam([(29, 33.5), (55, 33.5)], style, palette))            # head / neck
        d.append(seam([(27, 39.5), (59, 39.5)], style, palette))            # neck / chest
        d.append(seam([(29, 50), (57, 50)], style, palette))                # chest / apron
        d.append(seam([(56, 45.5), (60.5, 38.5)], style, palette))          # chest / arm
        d.append(seam([(21.5, 44), (28.5, 44)], style, palette))            # chest / far arm
        d.append(seam([(43, 71), (43, 87)], style, palette))                # between legs
        d.append(seam([(30, 71.5), (56, 71.5)], style, palette))            # apron / legs
        d.append(seam([(30, 86), (58, 86)], style, palette))                # legs / shoes
        d.append(seam([(84, 26), (88, 23)], style, palette))                # arm / saucer

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

    if style.key == "seg":
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
    sys.exit("sprites: waiting for the art-direction choice (run `sheet` first)")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="cmd", required=True)
    sub.add_parser("sheet", help="render the art-direction comparison sheet").set_defaults(fn=cmd_sheet)
    sub.add_parser("sprites", help="render the game sprites in the chosen style").set_defaults(fn=cmd_sprites)
    args = parser.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
