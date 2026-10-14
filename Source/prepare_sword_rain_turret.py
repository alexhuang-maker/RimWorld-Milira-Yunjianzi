from pathlib import Path
import sys

from PIL import Image


def square_crop(image: Image.Image, half: tuple[int, int, int, int], padding: int = 24) -> Image.Image:
    region = image.crop(half)
    alpha = region.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError("No non-transparent pixels found in generated turret half")

    left, top, right, bottom = bbox
    width = right - left
    height = bottom - top
    side = max(width, height) + padding * 2
    center_x = (left + right) // 2
    center_y = (top + bottom) // 2
    crop_left = center_x - side // 2
    crop_top = center_y - side // 2

    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    source_box = (
        max(0, crop_left),
        max(0, crop_top),
        min(region.width, crop_left + side),
        min(region.height, crop_top + side),
    )
    fragment = region.crop(source_box)
    canvas.paste(fragment, (source_box[0] - crop_left, source_box[1] - crop_top), fragment)
    return canvas.resize((512, 512), Image.Resampling.LANCZOS)


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: prepare_sword_rain_turret.py TRANSPARENT_SHEET OUTPUT_DIR")

    source = Image.open(sys.argv[1]).convert("RGBA")
    output = Path(sys.argv[2])
    output.mkdir(parents=True, exist_ok=True)
    midpoint = source.width // 2

    base = square_crop(source, (0, 0, midpoint, source.height))
    top = square_crop(source, (midpoint, 0, source.width, source.height))
    base.save(output / "SwordRainTurret_Base.png", optimize=True)
    top.save(output / "SwordRainTurret_Top.png", optimize=True)


if __name__ == "__main__":
    main()
