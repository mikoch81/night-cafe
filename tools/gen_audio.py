#!/usr/bin/env python3
"""Synthesise every Night Café sound effect and the lo-fi bed.

The GDD (5.3) specifies each sound; generating them here keeps the licence
unambiguous - nothing is downloaded. Standard library only, so this runs anywhere.

Output: 44.1 kHz, 16-bit, mono WAV files in Assets/Audio/.
"""

import array
import math
import os
import random
import struct
import wave

SR = 44100
OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Audio")


# ---------------------------------------------------------------- helpers

def silence(seconds):
    return [0.0] * int(SR * seconds)


def env_ad(n, attack_s, tau_s):
    """Linear attack into an exponential decay."""
    attack = max(1, int(SR * attack_s))
    out = []
    for i in range(n):
        if i < attack:
            out.append(i / attack)
        else:
            out.append(math.exp(-(i - attack) / (SR * tau_s)))
    return out


def blsquare(freq, t):
    """Band-limited square: odd harmonics up to 15 kHz, so it does not alias."""
    value = 0.0
    k = 1
    while k * freq < 15000:
        value += math.sin(2 * math.pi * k * freq * t) / k
        k += 2
    return value * (4 / math.pi)


def blsaw(freq, t, fmax=8000):
    value = 0.0
    k = 1
    while k * freq < fmax:
        value += math.sin(2 * math.pi * k * freq * t) / k
        k += 1
    return -value * (2 / math.pi)


def onepole_lp(buf, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / SR)
    y = 0.0
    out = []
    for x in buf:
        y += a * (x - y)
        out.append(y)
    return out


def normalise(buf, peak_dbfs=-3.0):
    peak = max((abs(v) for v in buf), default=0.0)
    if peak == 0:
        return buf
    target = 10 ** (peak_dbfs / 20)
    gain = target / peak
    return [v * gain for v in buf]


def fade_edges(buf, ms=2):
    n = min(int(SR * ms / 1000), len(buf) // 2)
    for i in range(n):
        buf[i] *= i / n
        buf[-1 - i] *= i / n
    return buf


def mix(*layers):
    length = max(len(layer) for layer in layers)
    out = [0.0] * length
    for layer in layers:
        for i, v in enumerate(layer):
            out[i] += v
    return out


def write_wav(name, buf):
    path = os.path.join(OUT_DIR, name)
    data = array.array("h", (int(max(-1.0, min(1.0, v)) * 32767) for v in buf))
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(f"  {name:24} {len(buf) / SR:6.3f}s")


# ---------------------------------------------------------------- sounds

def sfx_catch():
    """GDD 5.3: blip 1050 Hz, square, short decay, 40 ms."""
    n = int(SR * 0.040)
    env = env_ad(n, 0.001, 0.008)
    buf = [blsquare(1050, i / SR) * env[i] * 0.5 for i in range(n)]
    return fade_edges(normalise(buf))


def sfx_combo():
    """GDD 2.4: a three note arpeggio every 25 catches - root, major third, fifth."""
    notes = [1050.0, 1312.5, 1575.0]
    note_len = 0.070
    buf = []
    for index, freq in enumerate(notes):
        n = int(SR * note_len)
        tau = 0.060 if index == len(notes) - 1 else 0.025
        env = env_ad(n, 0.002, tau)
        buf.extend(blsquare(freq, i / SR) * env[i] * 0.5 for i in range(n))

    buf.extend(silence(0.030))
    return fade_edges(normalise(buf))


def sfx_miss():
    """GDD 5.3: ceramic clink plus a 120 ms low buzz."""
    total = int(SR * 0.200)

    # Inharmonic partials in bar-mode ratios read as struck ceramic.
    clink = [0.0] * total
    for freq, tau, amp in ((2400, 0.045, 1.0), (6624, 0.030, 0.60), (12960, 0.018, 0.35)):
        env = env_ad(total, 0.0005, tau)
        for i in range(total):
            clink[i] += math.sin(2 * math.pi * freq * i / SR) * env[i] * amp

    rng = random.Random(20260729)
    chip = int(SR * 0.003)
    for i in range(chip):
        clink[i] += rng.uniform(-1, 1) * 0.5 * (1 - i / chip)

    buzz_n = int(SR * 0.120)
    buzz = []
    for i in range(buzz_n):
        t = i / SR
        am = 1 - 0.3 * (1 if math.sin(2 * math.pi * 35 * t) >= 0 else 0)
        if i < SR * 0.005:
            envelope = i / (SR * 0.005)
        elif i > buzz_n - SR * 0.030:
            envelope = (buzz_n - i) / (SR * 0.030)
        else:
            envelope = 1.0
        buzz.append(blsaw(70, t, fmax=4000) * am * envelope * 0.6)
    buzz.extend([0.0] * (total - buzz_n))

    return fade_edges(normalise(mix(clink, buzz)))


def sfx_cat():
    """GDD 5.3: a soft 300 ms 'mrau' - a formant sweep, not a square blip."""
    n = int(SR * 0.300)
    buf = []
    phase = 0.0
    for i in range(n):
        t = i / SR
        # Pitch rises then falls, the way a short meow does.
        freq = 420 + 140 * math.sin(math.pi * t / 0.300)
        phase += 2 * math.pi * freq / SR
        tone = math.sin(phase) + 0.35 * math.sin(2 * phase) + 0.15 * math.sin(3 * phase)

        if t < 0.040:
            envelope = t / 0.040
        elif t > 0.220:
            envelope = max(0.0, (0.300 - t) / 0.080)
        else:
            envelope = 1.0

        buf.append(tone * envelope * 0.4)

    return fade_edges(normalise(onepole_lp(buf, 2200)))


def sfx_gameover():
    """GDD 5.3: a falling third over 600 ms."""
    notes = [(523.25, 0.30), (415.30, 0.30)]  # C5 -> G#4
    buf = []
    for freq, length in notes:
        n = int(SR * length)
        env = env_ad(n, 0.005, 0.180)
        for i in range(n):
            t = i / SR
            tone = math.sin(2 * math.pi * freq * t) + 0.3 * blsquare(freq, t) * 0.3
            buf.append(tone * env[i] * 0.45)

    return fade_edges(normalise(buf))


def music_loop():
    """
    GDD 5.3: a 60-90 s lo-fi bed. 18 bars at 72 BPM lands on exactly 60.000 s, and the
    progression is written so the last bar leads back into the first without a seam.
    """
    bpm = 72.0
    bar = 4 * 60.0 / bpm  # 3.3333 s
    bars = 18
    total = int(SR * bar * bars)
    rng = random.Random(20260728)

    # ii - V - I - vi in A minor, two bars each, looping cleanly.
    chords = [
        (220.00, 261.63, 329.63),  # Am
        (174.61, 220.00, 261.63),  # F
        (196.00, 246.94, 293.66),  # G
        (164.81, 196.00, 246.94),  # Em
    ]

    pad = [0.0] * total
    for index in range(bars):
        chord = chords[(index // 2) % len(chords)]
        start = int(index * bar * SR)
        length = int(bar * SR)

        for i in range(length):
            if start + i >= total:
                break
            t = i / SR
            # Slow swell in and out so bar joins are inaudible.
            envelope = min(t / 0.5, 1.0, max(0.0, (bar - t) / 0.8))
            value = 0.0
            for voice, freq in enumerate(chord):
                # Rhodes-ish: sine plus a quiet second partial, gently detuned per voice.
                detune = 1.0 + 0.0009 * (voice - 1)
                value += math.sin(2 * math.pi * freq * detune * t)
                value += 0.18 * math.sin(4 * math.pi * freq * detune * t)
            pad[start + i] += value * envelope * 0.16

    pad = onepole_lp(pad, 1800)

    # Vinyl crackle: sparse clicks plus a whisper of noise.
    crackle = [rng.uniform(-1, 1) * 0.006 for _ in range(total)]
    for _ in range(int(bars * 22)):
        at = rng.randrange(total - 400)
        amp = rng.uniform(0.05, 0.16)
        for i in range(rng.randint(60, 380)):
            crackle[at + i] += amp * math.exp(-i / 90) * rng.uniform(-1, 1)

    buf = normalise(mix(pad, crackle), peak_dbfs=-3.0)

    # A loop must start and end at zero or the seam clicks on every repeat.
    return fade_edges(buf, ms=12)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    print(f"Writing to {OUT_DIR}")

    write_wav("sfx_catch.wav", sfx_catch())
    write_wav("sfx_combo.wav", sfx_combo())
    write_wav("sfx_miss.wav", sfx_miss())
    write_wav("sfx_cat.wav", sfx_cat())
    write_wav("sfx_gameover.wav", sfx_gameover())
    write_wav("music_lofi_loop.wav", music_loop())


if __name__ == "__main__":
    main()
