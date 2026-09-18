#!/usr/bin/env python3
"""Cuts the closed-test trailer from a phone screen recording of one session.

    py -3.12 tools/make_trailer.py build/trailer/take1.mp4

Segments are (source in, out, caption) pairs below; the logo and the end card are drawn with
Pillow, the cut, the letterbox, the captions in the bottom band and the game's own lo-fi
record with the rain under it are ffmpeg. Output: build/trailer/night_cafe_trailer.mp4 (16:9,
1080p) and _story.mp4 (9:16 with the console centred and larger captions). No audio comes
from the recording - screenrecord has none - so the soundtrack is the ART sound set.
"""
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FFMPEG = os.environ.get("FFMPEG", r"C:\Users\Michał\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe")
OUT_DIR = os.path.join(ROOT, "build", "trailer")
FONT_HEAD = os.path.join(ROOT, "Assets", "Fonts", "CabinSketch-Bold.ttf")
FONT_BODY = os.path.join(ROOT, "Assets", "Fonts", "PatrickHand-Regular.ttf")
LOGO = os.path.join(ROOT, "Assets", "Art", "icon", "splash_logo.png")
MUSIC = os.path.join(ROOT, "Assets", "Audio", "Art", "music_lofi_loop.wav")
RAIN = os.path.join(ROOT, "Assets", "Audio", "Art", "ambience_rain_cafe.wav")
BG = (0x12, 0x0b, 0x08)
BAND = (0x2d, 0x28, 0x24)      # the recording's own background around the console
AMBER = (255, 201, 102)
CREAM = (246, 234, 210)
LINK = "play.google.com/apps/testing/com.mikoch81.nightcafe"

# (in, out) seconds in the take, and the caption shown in the band below the console
SEGMENTS = [
    (38.5, 42.5, "Nocna kawiarnia w kieszeni"),
    (78.0, 90.3, "Łap kubki na tacę, zanim spadną z lady"),
    (90.3, 94.5, "Trzy stłuczki i koniec zmiany"),
    (102.0, 113.0, "Tryb B: podawaj tylko kolor z zamówienia"),
]
LOGO_SECONDS = 2.5
END_SECONDS = 4.5


def ff(*args):
    subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def logo_card(size):
    w, h = size
    card = Image.new("RGB", size, BG)
    logo = Image.open(LOGO).convert("RGBA")
    scale = min(w * 0.72 / logo.width, h * 0.3 / logo.height)
    logo = logo.resize((int(logo.width * scale), int(logo.height * scale)), Image.LANCZOS)
    card.paste(logo, ((w - logo.width) // 2, (h - logo.height) // 2), logo)
    return card


def end_card(size, portrait):
    w, h = size
    card = Image.new("RGB", size, BG)
    draw = ImageDraw.Draw(card)
    head = ImageFont.truetype(FONT_HEAD, int(h * (0.075 if portrait else 0.12)))
    body = ImageFont.truetype(FONT_BODY, int(h * (0.032 if portrait else 0.052)))
    small = ImageFont.truetype(FONT_BODY, int(h * (0.024 if portrait else 0.04)))
    lines = [
        ("NIGHT CAFÉ", head, AMBER, 0.28),
        ("Android · za darmo · bez reklam · offline", body, CREAM, 0.44),
        ("Zamknięty test — dołącz na Gmailu:", body, CREAM, 0.56),
        (LINK, small, AMBER, 0.65),
        ("wystarczy kliknąć „Zostań testerem” (działa też na iPhonie)", small, (170, 150, 120), 0.74),
    ]
    for text, font, colour, y in lines:
        tw = draw.textlength(text, font=font)
        draw.text(((w - tw) / 2, h * y), text, font=font, fill=colour)
    return card


def build(take, portrait):
    size = (1080, 1920) if portrait else (1920, 1080)
    w, h = size
    tag = "_story" if portrait else ""
    os.makedirs(OUT_DIR, exist_ok=True)
    logo_png = os.path.join(OUT_DIR, f"card_logo{tag}.png")
    end_png = os.path.join(OUT_DIR, f"card_end{tag}.png")
    logo_card(size).save(logo_png)
    end_card(size, portrait).save(end_png)

    # the console frame: full width, letterboxed; captions sit in the band under it
    frame_h = round(w * 1080 / 2424 / 2) * 2
    cap_size = int(h * (0.034 if portrait else 0.052))
    cap_y = (h + frame_h) // 2 + (int(h * 0.06) if portrait else (h - frame_h) // 4 - cap_size // 2)
    # Patrick Hand carries the Polish glyphs; Cabin Sketch (the headings) does not
    font_path = FONT_BODY.replace("\\", "/").replace(":", r"\:")
    band = "0x%02x%02x%02x" % BAND

    inputs = ["-loop", "1", "-t", str(LOGO_SECONDS), "-i", logo_png]
    filters = [f"[0:v]scale={w}:{h},fps=30,format=yuv420p,fade=t=in:st=0:d=0.6[v0]"]
    for i, (start, end, caption) in enumerate(SEGMENTS, start=1):
        inputs += ["-ss", str(start), "-t", str(end - start), "-i", take]
        text = caption.replace("\\", "\\\\").replace(":", r"\:").replace("'", r"\'")
        filters.append(
            f"[{i}:v]scale={w}:{frame_h},pad={w}:{h}:0:(oh-ih)/2:color={band},fps=30,format=yuv420p,"
            f"drawtext=fontfile='{font_path}':text='{text}':fontsize={cap_size}:fontcolor=0xffc966:"
            f"x=(w-text_w)/2:y={cap_y}:shadowcolor=0x000000@0.6:shadowx=2:shadowy=2[v{i}]")
    n = len(SEGMENTS) + 1
    inputs += ["-loop", "1", "-t", str(END_SECONDS), "-i", end_png]
    filters.append(f"[{n}:v]scale={w}:{h},fps=30,format=yuv420p,fade=t=out:st={END_SECONDS - 0.8}:d=0.8[v{n}]")
    chain = "".join(f"[v{i}]" for i in range(n + 1))
    filters.append(f"{chain}concat=n={n + 1}:v=1:a=0[video]")

    total = LOGO_SECONDS + sum(e - s for s, e, _ in SEGMENTS) + END_SECONDS
    inputs += ["-stream_loop", "-1", "-i", MUSIC, "-stream_loop", "-1", "-i", RAIN]
    filters.append(
        f"[{n + 1}:a]atrim=0:{total},volume=0.9,afade=t=in:st=0:d=1.5,afade=t=out:st={total - 2.5}:d=2.5[m];"
        f"[{n + 2}:a]atrim=0:{total},volume=0.35,afade=t=in:st=0:d=1,afade=t=out:st={total - 2.5}:d=2.5[r];"
        f"[m][r]amix=inputs=2:duration=first:normalize=0[audio]")

    out = os.path.join(OUT_DIR, f"night_cafe_trailer{tag}.mp4")
    ff(*inputs, "-filter_complex", ";".join(filters), "-map", "[video]", "-map", "[audio]",
       "-c:v", "libx264", "-preset", "slow", "-crf", "20", "-pix_fmt", "yuv420p",
       "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", "-t", str(total), out)
    print(f"{out}  {total:.1f} s")


def main():
    take = sys.argv[1] if len(sys.argv) > 1 else os.path.join(OUT_DIR, "take1.mp4")
    build(take, portrait=False)
    build(take, portrait=True)


if __name__ == "__main__":
    main()
