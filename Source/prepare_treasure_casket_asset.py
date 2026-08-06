from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Storage" / "TreasureCasket_Jianmu_Transparent.png"
OUTPUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Buildings" / "Storage" / "TreasureCasket.png"
QA = ROOT / "ArtSource" / "Storage" / "TreasureCasket_QA.png"


def alpha_bounds(image: Image.Image):
    return image.getchannel("A").getbbox()


def main():
    image = Image.open(SOURCE).convert("RGBA")
    bounds = alpha_bounds(image)
    if bounds is None:
        raise RuntimeError("Treasure casket source has no opaque pixels")

    subject = image.crop(bounds)
    target = 930
    scale = min(target / subject.width, target / subject.height)
    size = (max(1, round(subject.width * scale)), max(1, round(subject.height * scale)))
    subject = subject.resize(size, Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    # A few pixels lower than mathematical center makes the front face sit
    # naturally on the center of a RimWorld cell.
    position = ((1024 - size[0]) // 2, (1024 - size[1]) // 2 + 8)
    canvas.alpha_composite(subject, position)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUTPUT)

    checker = Image.new("RGBA", canvas.size, (24, 36, 45, 255))
    tile = 32
    pixels = checker.load()
    for y in range(0, checker.height, tile):
        for x in range(0, checker.width, tile):
            color = (37, 54, 66, 255) if ((x // tile) + (y // tile)) % 2 else (24, 36, 45, 255)
            for yy in range(y, min(y + tile, checker.height)):
                for xx in range(x, min(x + tile, checker.width)):
                    pixels[xx, yy] = color
    checker.alpha_composite(canvas)
    QA.parent.mkdir(parents=True, exist_ok=True)
    checker.save(QA)


if __name__ == "__main__":
    main()
