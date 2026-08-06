"""Apply deterministic, reviewable anchor fixes to Shenji raster layers."""

from pathlib import Path
from PIL import Image, ImageOps


MOD = Path(__file__).resolve().parents[1]
VISOR = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji" / "ShenjiVisor_Female_east.png"


def main() -> None:
    # Graphic_Multi mirrors east for west.  The imported source had the ear hinge
    # on the facial side, so mirror the canonical east texture exactly once; the
    # engine's west mirror then produces the opposite correct orientation.
    image = Image.open(VISOR).convert("RGBA")
    gold_x = []
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = image.getpixel((x, y))
            if alpha > 80 and red > 105 and green > 70 and blue < 105 and red > blue * 1.35:
                gold_x.append(x)
    hinge_center = sum(gold_x) / len(gold_x) if gold_x else image.width
    if hinge_center > image.width / 2:
        image = ImageOps.mirror(image)
        image.save(VISOR)
        print(f"mirrored {VISOR} (hinge x {hinge_center:.1f})")
    else:
        print(f"already correct {VISOR} (hinge x {hinge_center:.1f})")


if __name__ == "__main__":
    main()
