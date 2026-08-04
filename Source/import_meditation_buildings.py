"""Split the generated meditation-building sheet into game-ready sprites."""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/GeneratedAssets/MeditationBuildings/Yunjianzi_MeditationBuildings_v1_transparent.png"
OUTPUT = ROOT / "Content/Textures/Yunjianzi/Buildings/Meditation"
PREVIEW = ROOT / "ArtSource/GeneratedAssets/MeditationBuildings/Yunjianzi_MeditationBuildings_Preview_v1.png"


def extract(source: Image.Image, box: tuple[int, int, int, int], size: int) -> Image.Image:
    cell = source.crop(box)
    bounds = cell.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("Generated building cell contains no visible pixels")
    glyph = cell.crop(bounds)
    margin = max(glyph.width, glyph.height) // 20
    padded = Image.new("RGBA", (glyph.width + margin * 2, glyph.height + margin * 2))
    padded.alpha_composite(glyph, (margin, margin))
    padded.thumbnail((size, size), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size))
    canvas.alpha_composite(padded, ((size - padded.width) // 2, (size - padded.height) // 2))
    return canvas


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    split = int(source.width * 0.40)
    cushion = extract(source, (0, 0, split, source.height), 512)
    platform = extract(source, (split, 0, source.width, source.height), 1024)
    OUTPUT.mkdir(parents=True, exist_ok=True)
    cushion.save(OUTPUT / "MeditationCushion.png", optimize=True)
    platform.save(OUTPUT / "WhiteJadePlatform.png", optimize=True)

    preview = Image.new("RGBA", (1400, 760), (11, 25, 32, 255))
    draw = ImageDraw.Draw(preview)
    for y in range(0, preview.height, 40):
        for x in range(0, preview.width, 40):
            if (x // 40 + y // 40) % 2 == 0:
                draw.rectangle((x, y, x + 39, y + 39), fill=(18, 41, 49, 255))
    cushion_preview = cushion.resize((360, 360), Image.Resampling.LANCZOS)
    platform_preview = platform.resize((680, 680), Image.Resampling.LANCZOS)
    preview.alpha_composite(cushion_preview, (90, 200))
    preview.alpha_composite(platform_preview, (620, 40))
    preview.save(PREVIEW, optimize=True)
    print(PREVIEW)


if __name__ == "__main__":
    main()
