from pathlib import Path
import math

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Effects" / "BlinkSlash.png"


def crescent_polygon(width: int, height: int, thickness_scale: float = 1.0):
    upper = []
    lower = []
    for index in range(121):
        t = index / 120.0
        x = 22 + (width - 44) * t
        centre_y = height * 0.72 - math.sin(math.pi * t) * height * 0.43
        thickness = (1.2 + 15.5 * math.sin(math.pi * t) ** 1.5) * thickness_scale
        upper.append((x, centre_y - thickness))
        lower.append((x, centre_y + thickness))
    return upper + list(reversed(lower))


def main() -> None:
    width, height = 512, 256
    glow = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_draw.polygon(crescent_polygon(width, height, 2.5), fill=(35, 226, 255, 185))
    glow = glow.filter(ImageFilter.GaussianBlur(15))

    body = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(body)
    draw.polygon(crescent_polygon(width, height, 1.0), fill=(82, 226, 255, 235))
    draw.polygon(crescent_polygon(width, height, 0.38), fill=(246, 255, 255, 255))

    # A restrained secondary stroke and jade-like sparks make the instantaneous
    # cut readable without obscuring pawns beneath it.
    draw.arc((92, 58, 438, 238), start=202, end=334, fill=(128, 245, 255, 190), width=4)
    for x, y, size in ((96, 113, 5), (177, 54, 4), (348, 47, 5), (430, 105, 4), (393, 178, 3)):
        draw.polygon(((x, y - size), (x + size, y), (x, y + size), (x - size, y)), fill=(228, 255, 255, 235))

    result = Image.alpha_composite(glow, body)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    result.save(OUTPUT, optimize=True)
    print(OUTPUT)


if __name__ == "__main__":
    main()
