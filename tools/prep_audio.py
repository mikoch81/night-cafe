#!/usr/bin/env python3
"""Turn the generated recordings in art/audio/raw/ into the ART sound set in Assets/Audio/Art/.

    py -3.12 tools/prep_audio.py            # needs: numpy scipy soundfile pyloudnorm

The raw files come from Suno (the lo-fi track) and ElevenLabs (effects, rain); see
art/audio/LICENSE.md. This script does the cutting so the sources stay untouched:

  * effects   - trim silence, short fades, peak-normalise to -3 dBFS (plus a per-sound trim),
                48 kHz mono
  * music     - find a bar-aligned, seamless 60-90 s loop inside the track (GDD 5.3), crossfade
                the seam, integrated loudness -14 LUFS (the game then applies AudioConfig's
                music offset)
  * ambience  - chain the rain variants with crossfades into one loop, -16 LUFS

The synthesised RETRO set (tools/gen_audio.py -> Assets/Audio/) is not touched.
"""

import os
import sys

import numpy as np
import pyloudnorm as pyln
import soundfile as sf
from scipy import signal

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "art", "audio", "raw")
OUT = os.path.join(ROOT, "Assets", "Audio", "Art")
SR = 48000

# name -> (raw file, max seconds, trim dB): the trim balances sounds the generator delivered at
# very different levels (the meow came in 10 dB hotter than the cup).
SFX = {
    "sfx_catch": ("sfx_catch.wav", 0.6, -5.0),
    "sfx_miss": ("sfx_miss.wav", 1.6, -9.0),
    "sfx_combo": ("sfx_combo.wav", 1.0, -3.0),
    "sfx_cat": ("sfx_cat.wav", 1.0, -6.0),
    "sfx_gameover": ("sfx_gameover.wav", 2.0, -2.0),
    "sfx_brew_alarm": ("sfx_brew_alarm.wav", 1.0, -2.0),
    "sfx_click": ("sfx_click.wav", 0.4, -1.0),
}
MUSIC_RAW = "lofi_loop.wav"
AMBIENCE_RAW = ["ambience_rain_cafe.wav", "ambience_rain_cafe2.wav", "ambience_rain_cafe3.wav", "ambience_rain_cafe4.wav"]

LOOP_MIN_S, LOOP_MAX_S = 60.0, 90.0
MUSIC_LUFS = -14.0
AMBIENCE_LUFS = -16.0
SFX_PEAK_DB = -3.0


# ---------------------------------------------------------------- helpers

def load(name):
    data, sr = sf.read(os.path.join(RAW, name), always_2d=True, dtype="float64")
    if sr != SR:
        data = signal.resample_poly(data, SR, sr, axis=0)
    return data


def mono(data):
    return data.mean(axis=1)


def db(x):
    return 20.0 * np.log10(max(abs(x), 1e-12))


def gain(dbs):
    return 10.0 ** (dbs / 20.0)


def fade(buf, in_s=0.0, out_s=0.0):
    n_in, n_out = int(SR * in_s), int(SR * out_s)
    if n_in:
        buf[:n_in] *= np.linspace(0.0, 1.0, n_in)[:, None] if buf.ndim == 2 else np.linspace(0.0, 1.0, n_in)
    if n_out:
        ramp = np.linspace(1.0, 0.0, n_out)
        buf[-n_out:] *= ramp[:, None] if buf.ndim == 2 else ramp
    return buf


def trim(buf, threshold_db=-50.0, pre_s=0.005, post_s=0.05):
    thr = gain(threshold_db)
    idx = np.where(np.abs(buf) > thr)[0]
    if len(idx) == 0:
        return buf
    start = max(0, idx[0] - int(SR * pre_s))
    end = min(len(buf), idx[-1] + int(SR * post_s))
    return buf[start:end]


def loudness(buf):
    meter = pyln.Meter(SR)
    return meter.integrated_loudness(buf)


def to_lufs(buf, target):
    return buf * gain(target - loudness(buf))


def highpass(buf, hz=60.0):
    sos = signal.butter(2, hz, btype="highpass", fs=SR, output="sos")
    return signal.sosfiltfilt(sos, buf, axis=0)


def write(name, buf):
    os.makedirs(OUT, exist_ok=True)
    peak = np.abs(buf).max()
    if peak > 0.99:
        buf = buf / peak * 0.99
    sf.write(os.path.join(OUT, name + ".wav"), buf.astype(np.float32), SR, subtype="PCM_16")
    seconds = len(buf) / SR
    print(f"  {name}.wav  {seconds:6.2f} s  peak {db(np.abs(buf).max()):5.1f} dBFS")


# ---------------------------------------------------------------- effects

def effects():
    print("effects")
    for name, (raw, max_s, trim_db) in SFX.items():
        buf = trim(mono(load(raw)))
        buf = buf[: int(SR * max_s)]
        buf = fade(buf, 0.003, min(0.04, len(buf) / SR * 0.25))
        buf = buf / np.abs(buf).max() * gain(SFX_PEAK_DB + trim_db)
        write(name, buf)


# ---------------------------------------------------------------- music loop

def onset_envelope(x, hop=512, win=2048):
    f, t, s = signal.stft(x, SR, nperseg=win, noverlap=win - hop)
    mag = np.abs(s)
    flux = np.maximum(mag[:, 1:] - mag[:, :-1], 0.0).sum(axis=0)
    flux = np.concatenate([[0.0], flux])
    return flux / (flux.max() + 1e-9), hop


def estimate_bpm(env, hop, lo=60.0, hi=100.0):
    fps = SR / hop
    env = env - env.mean()
    ac = signal.correlate(env, env, mode="full")[len(env) - 1:]
    lags = np.arange(len(ac))
    mask = (lags >= fps * 60.0 / hi) & (lags <= fps * 60.0 / lo)
    lag = lags[mask][np.argmax(ac[mask])]
    return 60.0 * fps / lag


def seam_score(x, env, hop, a, b, beat):
    """How well the audio after b continues the audio after a (1 = identical): the onset
    pattern over two beats and the low band's waveform over one, averaged. The raw waveform
    alone scores every bar-aligned cut low (phase), the envelope alone cannot hear harmony."""
    n_env = int(2 * beat * SR / hop)
    ea, eb = env[a // hop:a // hop + n_env], env[b // hop:b // hop + n_env]
    ea, eb = ea - ea.mean(), eb - eb.mean()
    env_corr = float(np.dot(ea, eb) / (np.linalg.norm(ea) * np.linalg.norm(eb) + 1e-12))
    n = int(beat * SR)
    wa, wb = x[a:a + n], x[b:b + n]
    wave_corr = float(np.dot(wa, wb) / (np.linalg.norm(wa) * np.linalg.norm(wb) + 1e-12))
    return 0.5 * env_corr + 0.5 * wave_corr


def music():
    print("music")
    stereo = load(MUSIC_RAW)
    x = mono(stereo)
    env, hop = onset_envelope(x)
    bpm = estimate_bpm(env, hop)
    beat = 60.0 / bpm
    bar = 4 * beat
    print(f"  tempo {bpm:.2f} BPM (autocorrelation), bar {bar:.3f} s")

    # The seam is judged on a 2 kHz low band (bass and chords carry the harmony; the hats
    # only add phase noise). Candidate starts on a half-beat grid inside the first 24 s (past
    # any intro); lengths in whole bars within GDD's 60-90 s, each refined within +-0.2 s in
    # 5 ms steps because the tempo estimate is only good to ~1 BPM and 20 bars of drift is
    # most of a beat. The loop must end before the outro's fade.
    sos = signal.butter(4, 2000.0, btype="lowpass", fs=SR, output="sos")
    low = signal.sosfiltfilt(sos, x)
    fade_start = len(x) - int(SR * 6.0)
    best = None
    for bars in range(int(np.floor(LOOP_MIN_S / bar)), int(np.floor(LOOP_MAX_S / bar)) + 1):
        nominal = bars * bar
        for start_s in np.arange(0.0, 24.0, beat / 2):
            a = int(start_s * SR)
            for length_s in np.arange(nominal - 0.2, nominal + 0.2, 0.005):
                b = a + int(length_s * SR)
                if b + int(2 * beat * SR) > fade_start:
                    continue
                score = seam_score(low, env, hop, a, b, beat)
                if best is None or score > best[0]:
                    best = (score, a, b - a, bars)
    score, a, length, bars = best
    print(f"  loop: start {a / SR:.2f} s, {bars} bars = {length / SR:.2f} s "
          f"(=> {bars * 4 * 60.0 / (length / SR):.2f} BPM), seam match {score:.3f}")

    xfade = int(SR * 0.08)
    loop = stereo[a:a + length].copy()
    tail = stereo[a + length:a + length + xfade]        # what naturally follows the loop's end
    ramp = np.linspace(0.0, 1.0, xfade)[:, None]
    loop[:xfade] = loop[:xfade] * ramp + tail * (1.0 - ramp)
    loop = to_lufs(loop, MUSIC_LUFS)
    write("music_lofi_loop", loop)


# ---------------------------------------------------------------- ambience

def ambience():
    print("ambience")
    parts = [highpass(mono(load(name))) for name in AMBIENCE_RAW]
    xfade = int(SR * 1.0)
    out = parts[0]
    for part in parts[1:]:
        ramp = np.linspace(0.0, 1.0, xfade)
        joined = out[-xfade:] * (1.0 - ramp) + part[:xfade] * ramp
        out = np.concatenate([out[:-xfade], joined, part[xfade:]])
    # Close the loop the same way: the tail fades into a copy of the head.
    ramp = np.linspace(0.0, 1.0, xfade)
    out[:xfade] = out[:xfade] * ramp + out[-xfade:] * (1.0 - ramp)
    out = out[:-xfade]
    out = to_lufs(out, AMBIENCE_LUFS)
    write("ambience_rain_cafe", out)


if __name__ == "__main__":
    which = sys.argv[1:] or ["effects", "music", "ambience"]
    for name in which:
        globals()[name]()
