#!/usr/bin/env python3
"""Renders the launcher icon and splash logo: a neon-lit cup in the game's ink-and-amber line.

    py -3.12 tools/gen_icon.py

Writes to Assets/Art/icon/:
  icon_fg.png       432x432 adaptive-icon foreground (the cup inside the 66 % safe circle)
  icon_bg.png       432x432 adaptive-icon background (walnut with a low amber glow)
  icon_legacy.png   512x512 the two composed: legacy launcher icon and the Play listing icon
  splash_logo.png   1400x480 the cup and NIGHT CAFÉ in Cabin Sketch for the boot splash
SVG sources next to the PNGs in Assets/Art/Source/icon. Inkscape for the SVG, Pillow for the
wordmark (Inkscape cannot see a font that is not installed; Pillow reads the .ttf directly).
"""
import os
import shutil
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "icon")
SVG = os.path.join(ROOT, "Assets", "Art", "Source", "icon")
FONT = os.path.join(ROOT, "Assets", "Fonts", "CabinSketch-Bold.ttf")

INK = "#2b1d13"
AMBER = "#ffc966"
BRIGHT = "#ffd27a"
CREAM = "#f6ead2"
WALNUT = "#2a1a12"
WALNUT_DEEP = "#1a100b"


def cup_svg(size, glow=True):
    """The cup from the counter, side view, handle to the right, two curls of steam, on a
    neon glow. Drawn in a 432 box; the adaptive safe zone is the inner 288 circle."""
    c = size / 2
    s = size / 432  # everything below is authored in the 432 box
    def u(v):
        return f"{v * s:.2f}"
    glow_el = ""
    if glow:
        glow_el = f'''
  <defs>
    <radialGradient id="g" cx="50%" cy="58%" r="45%">
      <stop offset="0" stop-color="{AMBER}" stop-opacity="0.55"/>
      <stop offset="0.6" stop-color="{AMBER}" stop-opacity="0.12"/>
      <stop offset="1" stop-color="{AMBER}" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <circle cx="{u(216)}" cy="{u(232)}" r="{u(150)}" fill="url(#g)"/>'''
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 {size} {size}">{glow_el}
  <g stroke="{INK}" stroke-width="{u(11)}" stroke-linejoin="round" stroke-linecap="round" fill="none">
    <!-- saucer -->
    <path d="M {u(112)} {u(298)} Q {u(216)} {u(330)} {u(320)} {u(298)}" fill="{AMBER}"/>
    <!-- handle -->
    <path d="M {u(286)} {u(196)} C {u(348)} {u(186)} {u(348)} {u(262)} {u(286)} {u(268)}" stroke-width="{u(24)}" stroke="{INK}"/>
    <path d="M {u(286)} {u(196)} C {u(348)} {u(186)} {u(348)} {u(262)} {u(286)} {u(268)}" stroke-width="{u(7)}" stroke="{AMBER}"/>
    <!-- body -->
    <path d="M {u(136)} {u(172)} L {u(154)} {u(286)} Q {u(216)} {u(304)} {u(278)} {u(286)} L {u(296)} {u(172)} Z" fill="{AMBER}"/>
    <!-- rim -->
    <ellipse cx="{u(216)}" cy="{u(172)}" rx="{u(80)}" ry="{u(16)}" fill="{CREAM}"/>
    <ellipse cx="{u(216)}" cy="{u(174)}" rx="{u(62)}" ry="{u(9)}" fill="{INK}" stroke="none"/>
    <!-- steam -->
    <path d="M {u(190)} {u(140)} c -14 -18 14 -30 0 -50 c -10 -14 6 -22 2 -34" stroke="{BRIGHT}" stroke-width="{u(9)}"/>
    <path d="M {u(240)} {u(136)} c -14 -18 14 -30 0 -50 c -10 -14 6 -22 2 -34" stroke="{BRIGHT}" stroke-width="{u(9)}"/>
  </g>
</svg>
'''


def bg_svg(size):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 {size} {size}">
  <defs>
    <linearGradient id="w" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{WALNUT}"/>
      <stop offset="1" stop-color="{WALNUT_DEEP}"/>
    </linearGradient>
    <radialGradient id="n" cx="50%" cy="62%" r="55%">
      <stop offset="0" stop-color="{AMBER}" stop-opacity="0.22"/>
      <stop offset="1" stop-color="{AMBER}" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect width="{size}" height="{size}" fill="url(#w)"/>
  <g stroke="#3a261a" stroke-width="{size * 0.006:.2f}" opacity="0.8">
    <path d="M 0 {size*0.18:.0f} Q {size*0.5:.0f} {size*0.14:.0f} {size} {size*0.2:.0f}" fill="none"/>
    <path d="M 0 {size*0.46:.0f} Q {size*0.5:.0f} {size*0.5:.0f} {size} {size*0.44:.0f}" fill="none"/>
    <path d="M 0 {size*0.78:.0f} Q {size*0.5:.0f} {size*0.74:.0f} {size} {size*0.8:.0f}" fill="none"/>
  </g>
  <rect width="{size}" height="{size}" fill="url(#n)"/>
</svg>
'''


def render(svg_text, name, width, height):
    os.makedirs(SVG, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    svg_path = os.path.join(SVG, name + ".svg")
    png_path = os.path.join(OUT, name + ".png")
    with open(svg_path, "w", encoding="utf-8") as f:
        f.write(svg_text)
    subprocess.run(["inkscape", svg_path, "--export-type=png", f"--export-filename={png_path}",
                    f"--export-width={width}", f"--export-height={height}", "--export-background-opacity=0"],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return png_path


def main():
    if not shutil.which("inkscape"):
        sys.exit("inkscape not on PATH (C:\Program Files\Inkscape\bin)")

    # Foreground authored for the 432 canvas: the 66 % safe circle is r=144 around the centre;
    # the cup with its saucer spans roughly 112..348 x 60..330, so its corners sit just outside the
    # circle where round masks may clip the saucer tips - by design, like a coaster under a mask.
    fg = render(cup_svg(432), "icon_fg", 432, 432)
    bg = render(bg_svg(432), "icon_bg", 432, 432)

    legacy = Image.open(bg).convert("RGBA").resize((512, 512), Image.LANCZOS)
    cup = Image.open(fg).convert("RGBA").resize((512, 512), Image.LANCZOS)
    legacy.alpha_composite(cup)
    legacy.save(os.path.join(OUT, "icon_legacy.png"))

    # Splash: cup on the left, the wordmark to its right, transparent ground (the splash
    # background colour comes from PlayerSettings).
    cup_px = 400
    cup_img = Image.open(render(cup_svg(432, glow=True), "splash_cup", cup_px, cup_px)).convert("RGBA")
    font = ImageFont.truetype(FONT, 190)
    text = "NIGHT CAFÉ"
    tw = int(font.getlength(text))
    w, h = cup_px + 40 + tw + 60, 480
    splash = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    splash.alpha_composite(cup_img, (20, (h - cup_px) // 2))
    draw = ImageDraw.Draw(splash)
    x, y = cup_px + 60, h // 2 - 110
    draw.text((x + 6, y + 6), text, font=font, fill=(0, 0, 0, 110))
    draw.text((x, y), text, font=font, fill=AMBER)
    splash.save(os.path.join(OUT, "splash_logo.png"))
    os.remove(os.path.join(OUT, "splash_cup.png"))
    print(f"icon_fg/bg 432, icon_legacy 512, splash_logo {w}x{h} -> {OUT}")


if __name__ == "__main__":
    main()
