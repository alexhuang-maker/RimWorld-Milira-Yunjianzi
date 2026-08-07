"""Extract the three transparent strategic-spell cast talismans.

The image model supplies the ornament vocabulary; this deterministic pass
removes the chroma field, keeps only luminous strokes, and places every glyph
on the same 256px runtime canvas so C# can hold it still for one second.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "GeneratedAssets" / "Yangfu" / "Yangfu_CastTalismans_v2_chroma.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Effects" / "Yangfu"
NAMES = ("TalismanRed", "TalismanPurple", "TalismanGreen")


def remove_magenta(image: Image.Image, index: int) -> Image.Image:
    result = image.convert("RGBA")
    pixels = result.load()
    palettes = ((255, 72, 34), (177, 88, 255), (74, 240, 150))
    base = palettes[index]
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, a = pixels[x, y]
            # Every desired stroke contains green/white luminance while the
            # magenta field does not. Fire also has a red-orange edge, so its
            # red-minus-blue contrast supplements the same mask.
            signal = g - 145
            if index == 0:
                signal = max(signal, (r - b) - 86)
            alpha = max(0, min(255, round(signal * 5.2)))
            if alpha == 0:
                pixels[x, y] = (255, 255, 255, 0)
            else:
                # Recolour the surviving linework so no magenta chroma fringe
                # can remain. Strong cores approach white; soft glow keeps the
                # spell-specific red, violet or green hue.
                white = min(.72, alpha / 255 * .62)
                pixels[x, y] = (
                    round(base[0] * (1 - white) + 255 * white),
                    round(base[1] * (1 - white) + 255 * white),
                    round(base[2] * (1 - white) + 255 * white),
                    min(a, alpha),
                )
    return result


def runtime_cell(image: Image.Image) -> Image.Image:
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError("Generated talisman cell is empty")
    subject = image.crop(bbox)
    subject.thumbnail((126, 226), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (256, 256))
    canvas.alpha_composite(subject, ((256 - subject.width) // 2, (256 - subject.height) // 2))
    return canvas


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    OUT.mkdir(parents=True, exist_ok=True)
    for index, name in enumerate(NAMES):
        left = round(index * source.width / 3)
        right = round((index + 1) * source.width / 3)
        cell = remove_magenta(source.crop((left, 0, right, source.height)), index)
        runtime_cell(cell).save(OUT / f"{name}.png", optimize=True)


if __name__ == "__main__":
    main()
