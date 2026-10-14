"""Import the generated no-wrap Shenji bob into Milira's directional hair slots."""

from pathlib import Path

from PIL import Image


MOD_ROOT = Path(__file__).resolve().parents[1]
SOURCE = MOD_ROOT / "ArtSource" / "GeneratedAssets" / "Shenji" / "ShenjiShortHair_NoWrap_v2_transparent.png"
OUTPUT = MOD_ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "Shenji"
CELL = 512


def source_cell(column: int, row: int) -> Image.Image:
    sheet = Image.open(SOURCE).convert("RGBA")
    return sheet.crop((column * CELL, row * CELL, (column + 1) * CELL, (row + 1) * CELL))


def fit_to_box(image: Image.Image, target_box: tuple[int, int, int, int]) -> Image.Image:
    bounds = image.getbbox()
    if bounds is None:
        return Image.new("RGBA", (CELL, CELL))

    art = image.crop(bounds)
    left, top, right, bottom = target_box
    target_width = right - left
    target_height = bottom - top
    scale = min(target_width / art.width, target_height / art.height)
    size = (round(art.width * scale), round(art.height * scale))
    art = art.resize(size, Image.Resampling.LANCZOS)

    center_x = (left + right) / 2
    center_y = (top + bottom) / 2
    position = (round(center_x - size[0] / 2), round(center_y - size[1] / 2))
    canvas = Image.new("RGBA", (CELL, CELL))
    canvas.alpha_composite(art, position)
    return canvas


def save(image: Image.Image, name: str) -> None:
    image.save(OUTPUT / name)


def main() -> None:
    # Preserve the native Shenji/Milira head anchors. The art is newly drawn;
    # these boxes only convert the generated sheet to the runtime sprite scale.
    # These are the exact visible bounds of the previously shipped in-game
    # Shenji hairstyle. They are the trusted scale and anchor reference.
    back_south = fit_to_box(source_cell(0, 0), (164, 161, 350, 343))
    front_south = fit_to_box(source_cell(0, 1), (164, 161, 349, 326))
    front_east = fit_to_box(source_cell(1, 1), (159, 161, 328, 335))
    front_north = fit_to_box(source_cell(2, 1), (164, 161, 350, 343))
    empty = Image.new("RGBA", (CELL, CELL))

    save(back_south, "ShenjiHairBack.png")
    save(back_south, "ShenjiHairBack_south.png")
    save(empty, "ShenjiHairBack_east.png")
    save(empty, "ShenjiHairBack_north.png")
    save(front_south, "ShenjiHairFront.png")
    save(front_south, "ShenjiHairFront_south.png")
    save(front_east, "ShenjiHairFront_east.png")
    save(front_north, "ShenjiHairFront_north.png")


if __name__ == "__main__":
    main()
