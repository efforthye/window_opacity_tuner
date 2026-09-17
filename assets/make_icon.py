#!/usr/bin/env python3
"""
Regenerates src/WindowOpacityTuner/app.ico.

Drawn rather than hand-authored so the icon can be tweaked in one place and the
whole size ladder stays consistent. Run with Pillow installed:

    python3 assets/make_icon.py
"""
import pathlib

from PIL import Image, ImageDraw

CANVAS = 1024
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]
OUT = pathlib.Path(__file__).resolve().parent.parent / "src/WindowOpacityTuner/app.ico"

SLATE_TOP = (76, 82, 92)
SLATE_BOTTOM = (42, 46, 52)


def rounded(size, radius, fill):
    """A rounded-rect mask-backed layer at canvas resolution."""
    layer = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    ImageDraw.Draw(layer).rounded_rectangle(size, radius=radius, fill=fill)
    return layer


def background():
    grad = Image.new("RGBA", (CANVAS, CANVAS))
    pixels = grad.load()
    for y in range(CANVAS):
        t = y / (CANVAS - 1)
        colour = tuple(int(a + ((b - a) * t)) for a, b in zip(SLATE_TOP, SLATE_BOTTOM))
        for x in range(CANVAS):
            pixels[x, y] = colour + (255,)

    mask = Image.new("L", (CANVAS, CANVAS), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, CANVAS - 1, CANVAS - 1), radius=224, fill=255)
    grad.putalpha(mask)
    return grad


def window_glyph():
    """A window that fades out left to right — the thing the app does, in one shape."""
    box = (224, 256, 800, 768)
    glyph = rounded(box, 48, (255, 255, 255, 255))

    # Title bar, a shade darker so the shape still reads as a window at 16px.
    bar = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    ImageDraw.Draw(bar).rounded_rectangle((box[0], box[1], box[2], box[1] + 140), radius=48, fill=(158, 166, 178, 255))
    ImageDraw.Draw(bar).rectangle((box[0], box[1] + 92, box[2], box[1] + 140), fill=(158, 166, 178, 255))
    glyph.alpha_composite(bar)

    # Horizontal alpha ramp: opaque on the left, nearly gone on the right.
    ramp = Image.new("L", (CANVAS, CANVAS), 0)
    ramp_pixels = ramp.load()
    left, right = box[0], box[2]
    for x in range(CANVAS):
        t = min(1.0, max(0.0, (x - left) / (right - left)))
        value = int(255 - (t * 200))
        for y in range(CANVAS):
            ramp_pixels[x, y] = value

    alpha = glyph.getchannel("A").point(lambda v: v)
    glyph.putalpha(Image.eval(Image.merge("L", (alpha,)), lambda v: v).point(lambda v: v))
    glyph.putalpha(Image.composite(ramp, Image.new("L", (CANVAS, CANVAS), 0), alpha))
    return glyph


def main():
    art = background()
    art.alpha_composite(window_glyph())

    frames = [art.resize((s, s), Image.LANCZOS) for s in SIZES]
    frames[-1].save(OUT, format="ICO", sizes=[(s, s) for s in SIZES], append_images=frames[:-1])
    print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
