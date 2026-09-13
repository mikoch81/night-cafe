#!/usr/bin/env python3
"""Vector props for the painted screen (GDD 5.2, ScreenStyle "ART"): the stain, the order board,
the footstool and the scoreboard. The cups, the broken cup and the shelf plank were vectors too
until Michał's Midjourney sheets (prompts 16/17) replaced them; those builders stay as a fallback
(`legacy_sprites`, rendered only by name).

    python3 tools/gen_art_v3.py            # everything -> Assets/Art/screen_v3
    python3 tools/gen_art_v3.py cup_latte plank

Ink line with flat colour fills, the same language as the Midjourney character sheets
(docs/references/09_ink_*). Order colours are read from ModeConfig.asset so a cup is exactly
the hex the GDD (section 3) names. Rendered at 8x through Inkscape (rsvg-convert as fallback)
because the painted set is imported at 200 pixels per unit. Standard library only.
"""
import argparse
import os
import re
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Art", "screen_v3")
SVG_DIR = os.path.join(ROOT, "Assets", "Art", "Source", "screen_v3")
MODE_ASSET = os.path.join(ROOT, "Assets", "Settings", "ModeConfigB.asset")
SCALE = 8

INK = "#2b1a12"
INK_SOFT = "#4a2e1f"
COFFEE = "#3a2216"
CREAM = "#f1e3c6"
WALNUT_TOP = "#9a6a3c"
WALNUT_FRONT = "#5e3a22"
BRASS = "#c9a24a"
STROKE = 2.2

ORDER_NAMES = ["espresso", "caramel", "latte", "decaf"]
ORDER_FALLBACK = ["#ffc966", "#ff9d6e", "#ffe9a8", "#d98cff"]


def order_colours():
    """orderColors from ModeConfigB.asset (Mode B is the one with orders); GDD hex as fallback."""
    if not os.path.exists(MODE_ASSET):
        return ORDER_FALLBACK
    text = open(MODE_ASSET, encoding="utf-8").read()
    m = re.search(r"orderColors:\n((?:\s*- \{r: [\d.]+, g: [\d.]+, b: [\d.]+, a: [\d.]+\}\n)+)", text)
    if not m:
        return ORDER_FALLBACK
    colours = []
    for r, g, b in re.findall(r"\{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)", m.group(1)):
        colours.append("#%02x%02x%02x" % tuple(round(float(v) * 255) for v in (r, g, b)))
    return colours if len(colours) == 4 else ORDER_FALLBACK


# ---------------------------------------------------------------- svg helpers

PAINT_DEFS = """
  <filter id="grain" x="-5%" y="-5%" width="110%" height="110%">
    <feTurbulence type="fractalNoise" baseFrequency="1.1" numOctaves="2" seed="5" result="noise"/>
    <feColorMatrix in="noise" type="saturate" values="0" result="grey"/>
    <feComponentTransfer in="grey" result="soft"><feFuncA type="table" tableValues="0 0.16"/></feComponentTransfer>
    <feComposite in="soft" in2="SourceGraphic" operator="in" result="clipped"/>
    <feBlend in="SourceGraphic" in2="clipped" mode="multiply"/>
  </filter>
  <filter id="wobble" x="-5%" y="-5%" width="110%" height="110%">
    <feTurbulence type="fractalNoise" baseFrequency="0.06" numOctaves="2" seed="9" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="1.1" xChannelSelector="R" yChannelSelector="G"/>
  </filter>
"""


def svg(width, height, body, defs="", painted=True):
    """`painted`: the whole drawing goes through a hand-drawn wobble and a paper grain."""
    group = 'filter="url(#wobble)"' if painted else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}">\n'
            f'<defs>{PAINT_DEFS if painted else ""}{defs}</defs>\n'
            f'<g stroke-linecap="round" stroke-linejoin="round" {group}>\n{body}</g>\n</svg>\n')


def gradient(gid, top, bottom, x1=0, y1=0, x2=0, y2=1):
    return (f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">'
            f'<stop offset="0" stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient>')


def lighter(hex_colour, factor=1.18):
    r, g, b = (int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))
    return "#%02x%02x%02x" % (min(255, int(r * factor)), min(255, int(g * factor)), min(255, int(b * factor)))


def shape(d, fill, stroke=INK, width=STROKE, extra=""):
    return f'  <path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{width}" {extra}/>\n'


def line(d, stroke=INK, width=STROKE, extra=""):
    return shape(d, "none", stroke, width, extra)


def rrect_path(x, y, w, h, r):
    return (f"M{x + r} {y} h{w - 2 * r} a{r} {r} 0 0 1 {r} {r} v{h - 2 * r} a{r} {r} 0 0 1 -{r} {r} "
            f"h-{w - 2 * r} a{r} {r} 0 0 1 -{r} -{r} v-{h - 2 * r} a{r} {r} 0 0 1 {r} -{r} z")


def ellipse_path(cx, cy, rx, ry):
    return f"M{cx - rx} {cy} a{rx} {ry} 0 1 0 {2 * rx} 0 a{rx} {ry} 0 1 0 -{2 * rx} 0 z"


def darker(hex_colour, factor=0.72):
    r, g, b = (int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))
    return "#%02x%02x%02x" % (int(r * factor), int(g * factor), int(b * factor))


# ---------------------------------------------------------------- props

def cup(colour):
    """Canvas 52x34, pivot (0.5, 0.60) like the LCD cup so LaneConfig steps need no change.
    A glazed ceramic cup in the order colour, seen a little from above: gouache-style shading
    (a gradient, a shade band, a dry-brush highlight), the coffee with a reflection, paper grain."""
    defs = (gradient("body", lighter(colour, 1.12), darker(colour, 0.68)) +
            gradient("rim", lighter(colour, 1.22), colour) +
            gradient("coffee", "#5a3a26", "#2a1710"))
    b = []
    b.append(shape("M5 11 q0 -4 5 -4 h22 q5 0 5 4 l-2 15 q-1 5 -6 5 h-16 q-5 0 -6 -5 z", "url(#body)", INK, 2.4))   # body
    b.append(shape("M37 13 h4 a6.5 6.5 0 0 1 0 13 h-5 l0.6 -4 h4 a2.5 2.5 0 0 0 0 -5 h-4 z", "url(#body)", INK, 2.2))  # handle
    b.append(shape("M11 26 q10 4 18 0 l-1 3 q-8 3 -16 0 z", darker(colour, 0.5), "none", 0, 'opacity="0.55"'))   # foot shade
    b.append(shape(ellipse_path(21, 11, 15.5, 4.2), "url(#rim)", INK, 2.2))                                  # rim (top face)
    b.append(shape(ellipse_path(21, 11.4, 12.5, 2.9), "url(#coffee)", INK, 1.4))                              # coffee
    b.append(shape(ellipse_path(16, 10.6, 4, 1.1), "#ffffff", "none", 0, 'opacity="0.22"'))                    # coffee reflection
    b.append(line("M8.5 14 q0.5 8 3 12.5", "#ffffff", 2.2, 'opacity="0.42"'))                                # dry-brush highlight
    b.append(line("M12 13.5 q0 4 1 7", "#ffffff", 1, 'opacity="0.3"'))
    b.append(line("M14 23 q7 2.5 14 0", darker(colour, 0.55), 1.6, 'opacity="0.55"'))                        # shade band
    b.append(shape("M5 11 q0 -4 5 -4 h22 q5 0 5 4 l-2 15 q-1 5 -6 5 h-16 q-5 0 -6 -5 z", "#000", "none", 0, 'filter="url(#grain)" opacity="0.10"'))
    return svg(52, 34, "".join(b), defs)


def cup_broken(colour=ORDER_FALLBACK[0]):
    """Canvas 52x40, pivot (0.5, 0.51): shards, a coffee puddle and drops."""
    b = []
    b.append(shape("M4 32 q20 8 44 0 q3 4 -2 6 q-20 5 -40 0 q-4 -3 -2 -6 z", COFFEE, INK, 1.6))       # puddle
    b.append(shape("M6 27 L10 12 L21 8 L24 19 L17 28 Z", colour))                                     # left shard
    b.append(shape("M26 24 L28 9 L39 13 L44 25 L37 30 Z", colour))                                    # right shard
    b.append(line("M12 15 l6 11", INK, 1.2))                                                          # crack
    b.append(line("M31 12 l3 14", INK, 1.2))
    b.append(shape(ellipse_path(46, 7, 2.4, 2.4), COFFEE, INK, 1.2))                                  # drops
    b.append(shape(ellipse_path(49, 16, 1.9, 1.9), COFFEE, INK, 1.2))
    b.append(shape(ellipse_path(7, 5, 1.7, 1.7), COFFEE, INK, 1.2))
    return svg(52, 40, "".join(b))


def stain():
    """Canvas 60x34, pivot (0.44, 0.39): a coffee puddle on the counter with a gloss."""
    b = []
    b.append(shape("M3 19 q8 -14 19 -6 q13 -10 21 2 q13 0 8 11 q3 9 -11 9 l-26 0 q-15 0 -11 -16z", COFFEE, INK, 1.8))
    b.append(shape(ellipse_path(20, 16, 5, 2.2), "#ffffff", "none", 0, 'opacity="0.28"'))
    b.append(shape(ellipse_path(41, 24, 3, 1.3), "#ffffff", "none", 0, 'opacity="0.18"'))
    return svg(60, 34, "".join(b))


def order_panel():
    """Canvas 130x52, centred pivot. A chalkboard sign in a wooden frame; the window at
    (14..50, 12..40) is where OrderPanelView draws the ordered cup, the name goes to the right."""
    b = []
    b.append(shape(rrect_path(2, 2, 126, 48, 8), WALNUT_FRONT, INK, 2.4))                             # frame
    b.append(shape(rrect_path(7, 7, 116, 38, 5), "#2a2d2b", INK, 1.6))                                # slate
    b.append(line("M10 10 h110", "#ffffff", 1, 'opacity="0.10"'))                                     # chalk dust
    b.append(line("M12 43 h106", "#ffffff", 1, 'opacity="0.06"'))
    b.append(shape(rrect_path(13, 11, 38, 30, 4), "#232624", "#e9e2cf", 1.2, 'opacity="0.9"'))        # chalk window
    b.append(line("M60 15 l0 22", "#e9e2cf", 1, 'opacity="0.35"'))                                    # divider
    return svg(130, 52, "".join(b))


PLANK_FRONT = "M3 12 h234 v13 q0 3 -3 3 h-228 q-3 0 -3 -3 z"
PLANK_TOP = "M3 12 l6 -8 h222 l6 8 z"


def plank_fill():
    """Mask for the wood photo texture: the plank's two faces in white (see render_plank)."""
    return svg(240, 30, shape(PLANK_FRONT, "#fff", "none", 0) + shape(PLANK_TOP, "#fff", "none", 0), painted=False)


def plank():
    """Canvas 240x30 rendered at 4x, 9-sliced (border 24 px each side, 96 px in the PNG): a wall shelf seen a little
    from above - lit top face, dark front, brass lip. The wood itself is a CC0 photo texture
    (ambientCG Wood027) laid under these lines by render_plank; this SVG is the ink and the light."""
    b = []
    b.append(shape(PLANK_TOP, "#fff3d6", "none", 0, 'opacity="0.22"'))                                 # top face catches the lamp
    b.append(shape(PLANK_FRONT, "#1a0e08", "none", 0, 'opacity="0.22"'))                               # front in shade
    b.append(shape("M3 12 h234 v3 h-234 z", "#000", "none", 0, 'opacity="0.3"'))                       # under the lip of the top face
    b.append(shape(PLANK_FRONT, "none", INK, 2.4))
    b.append(shape(PLANK_TOP, "none", INK, 2.4))
    b.append(line("M12 7.5 h216", "#ffffff", 1.6, 'opacity="0.3"'))                                    # sheen
    b.append(line("M6 24.5 h228", BRASS, 2.4))                                                         # brass lip
    b.append(line("M10 23.6 h220", lighter(BRASS, 1.25), 0.8, 'opacity="0.7"'))
    return svg(240, 30, "".join(b))


def scoreboard():
    """Canvas 110x36, centred pivot: the chalkboard the score is written on, framed in walnut."""
    b = []
    b.append(shape(rrect_path(2, 2, 106, 32, 5), WALNUT_FRONT, INK, 2.4))
    b.append(shape(rrect_path(6, 6, 98, 24, 3), "#23262a", INK, 1.6))
    b.append(shape(rrect_path(6, 6, 98, 24, 3), "#000", "none", 0, 'filter="url(#grain)" opacity="0.5"'))
    b.append(line("M12 27 q30 -2 86 0", "#e9e2cf", 1.2, 'opacity="0.12"'))                             # chalk smudge
    b.append(line("M14 9 h20", "#e9e2cf", 1, 'opacity="0.08"'))
    return svg(110, 36, "".join(b))


def step():
    """Canvas 60x200, pivot (0.5, 0.955) on the top platform: the step ladder Miro climbs for
    the upper shelves - tall enough to reach down behind the bar counter."""
    b = []
    b.append(shape("M12 12 l-6 184 h7 l5 -184 z", WALNUT_FRONT, INK, 2.2))                             # left leg
    b.append(shape("M48 12 l6 184 h-7 l-5 -184 z", WALNUT_FRONT, INK, 2.2))
    for y in (60, 105, 150):                                                                          # rungs
        b.append(shape(f"M9 {y} h42 v6 h-42 z", WALNUT_TOP, INK, 1.8))
    b.append(shape("M4 6 h52 l2 8 h-56 z", WALNUT_TOP, INK, 2.2))                                     # top platform
    b.append(line("M8 9 h44", "#ffffff", 1.2, 'opacity="0.25"'))
    return svg(60, 200, "".join(b))


def sprites():
    colours = order_colours()
    table = {
        "stain": (stain, 60, 34, SCALE),
        "order_panel": (order_panel, 130, 52, SCALE),
        "step": (step, 60, 200, SCALE),
        "scoreboard": (scoreboard, 110, 36, SCALE),
    }
    return table


def legacy_sprites():
    """The cups, the broken cup and the shelf plank now come from Midjourney (prompts 16 and 17,
    cut with tools/cut_sheet.py); these vector versions are kept as a fallback and render only
    when named explicitly, so a plain run never overwrites the painted files."""
    colours = order_colours()
    table = {
        "cup_broken": (lambda: cup_broken(colours[0]), 52, 40, SCALE),
        "plank": (plank, 240, 30, 4),   # 960x120 px = 0.6 units thick at 200 PPU, no squash in the sliced renderer
    }
    for name, colour in zip(ORDER_NAMES, colours):
        table[f"cup_{name}"] = ((lambda c: lambda: cup(c))(colour), 52, 34, SCALE)
    return table


# ---------------------------------------------------------------- rendering

def renderer():
    if shutil.which("inkscape"):
        return "inkscape"
    if shutil.which("rsvg-convert"):
        return "rsvg-convert"
    sys.exit("need inkscape or rsvg-convert")


def render(svg_path, png_path, width, height, tool, scale=SCALE):
    os.makedirs(os.path.dirname(png_path), exist_ok=True)
    if tool == "inkscape":
        cmd = ["inkscape", svg_path, "--export-type=png", f"--export-filename={png_path}",
               f"--export-width={width * scale}", f"--export-height={height * scale}",
               "--export-background-opacity=0"]
    else:
        cmd = ["rsvg-convert", "-w", str(width * scale), "-h", str(height * scale), "-o", png_path, svg_path]
    subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


WOOD_TEXTURE = os.path.join(ROOT, "art", "textures", "Wood027", "Wood027_1K-JPG_Color.jpg")


def render_plank(svg_path, png_path, width, height, tool, scale):
    """Wood photo texture (grain along the plank, painted look via ImageMagick's -paint), masked
    to the plank's faces, with the ink-and-light SVG on top."""
    w, h = width * scale, height * scale
    mask_svg = svg_path.replace(".svg", "_fill.svg")
    with open(mask_svg, "w", encoding="utf-8") as f:
        f.write(plank_fill())
    mask_png = png_path.replace(".png", "_fill.png")
    lines_png = png_path.replace(".png", "_lines.png")
    render(mask_svg, mask_png, width, height, tool, scale)
    render(svg_path, lines_png, width, height, tool, scale)
    if os.path.exists(WOOD_TEXTURE):
        subprocess.run(["magick", WOOD_TEXTURE, "-crop", "1024x160+0+120", "+repage", "-resize", f"{w}x{h}!",
                        "-paint", "1", "-modulate", "112,125,100", "-fill", "#a06a3a", "-colorize", "10",
                        "(", mask_png, "-alpha", "extract", ")", "-alpha", "off", "-compose", "CopyOpacity", "-composite",
                        lines_png, "-compose", "Over", "-composite", "PNG32:" + png_path], check=True)
    else:
        os.replace(lines_png, png_path)
    for tmp in (mask_png, lines_png, mask_svg):
        if os.path.exists(tmp):
            os.remove(tmp)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("only", nargs="*", help="sprite names (default: all)")
    args = ap.parse_args()
    table = sprites()
    tool = renderer()
    os.makedirs(SVG_DIR, exist_ok=True)
    legacy = legacy_sprites()
    for name in args.only or sorted(table):
        builder, w, h, scale = table[name] if name in table else legacy[name]
        svg_path = os.path.join(SVG_DIR, f"{name}.svg")
        with open(svg_path, "w", encoding="utf-8") as f:
            f.write(builder())
        if name == "plank":
            render_plank(svg_path, os.path.join(OUT_DIR, f"{name}.png"), w, h, tool, scale)
        else:
            render(svg_path, os.path.join(OUT_DIR, f"{name}.png"), w, h, tool, scale)
        print(f"{name}.png {w * scale}x{h * scale}")
    print(f"renderer: {tool}")


if __name__ == "__main__":
    main()
