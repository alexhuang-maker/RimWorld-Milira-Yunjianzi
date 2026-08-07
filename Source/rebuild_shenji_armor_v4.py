"""Build the three Shenji armor tiers from the fitted-arm 3x4 atlas.

The generated atlas is only a design source. This deterministic pass removes
the layout padding, scales every direction onto the native 512px Milian pawn
canvas, and writes the exact apparel paths loaded by RimWorld. The armour uses
native Milian construction: compact neutral limbs remain in the apparel
silhouette while the weapon is drawn independently by the carried node.
"""

from pathlib import Path

from PIL import Image


MOD = Path(__file__).resolve().parents[1]
ATLAS = MOD / "ArtSource" / "GeneratedAssets" / "Shenji" / "ShenjiArmor_Orthographic_FittedArms_v6_transparent.png"
OUTPUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"

ROWS = ("Base", "Intermediate", "Final")
COLUMNS = ("south", "east", "north", "west")

# Keep the generated armour's anatomy intact.  The previous pass forced every
# cell into a wide RimWorld box and visibly flattened the chest, hips and legs.
# These are vertical baselines plus a very small silhouette-specific width
# correction; width is derived from the original aspect ratio.
TARGET_HEIGHT = {"Base": 320, "Intermediate": 322, "Final": 330}
TARGET_BOTTOM = {"Base": 480, "Intermediate": 480, "Final": 482}
WIDTH_SCALE = {
    ("Base", "south"): 1.04, ("Base", "east"): 1.10, ("Base", "north"): 1.04,
    ("Intermediate", "south"): 1.04, ("Intermediate", "east"): 1.08, ("Intermediate", "north"): 1.04,
    ("Final", "south"): 1.00, ("Final", "east"): 1.04, ("Final", "north"): 1.00,
}


def source_cell(atlas: Image.Image, row: int, column: int) -> Image.Image:
    left = round(column * atlas.width / 4)
    right = round((column + 1) * atlas.width / 4)
    # Image generation leaves slightly larger breathing room above the first
    # row. Equal thirds cut the gold collar of the next row into the previous
    # cell (the visible crescent-under-the-boots bug), so use the actual bands.
    row_edges = (0.0, 0.325, 0.64, 1.0)
    top = round(row_edges[row] * atlas.height)
    bottom = round(row_edges[row + 1] * atlas.height)
    cell = atlas.crop((left, top, right, bottom))
    bounds = cell.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError(f"Empty armor cell row={row}, column={column}")
    return cell.crop(bounds)


def place(source: Image.Image, tier: str, direction: str) -> Image.Image:
    target_h = TARGET_HEIGHT[tier]
    natural_w = source.width * target_h / source.height
    target_w = round(natural_w * WIDTH_SCALE[(tier, direction)])
    fitted = source.resize((target_w, target_h), Image.Resampling.LANCZOS)
    # All three directions use the same neck/foot axis.  In profile the source
    # contains a trailing cape, but centring the complete silhouette still puts
    # the collar directly under the native Milian head.
    x = round(256 - target_w / 2)
    y = TARGET_BOTTOM[tier] - target_h
    canvas = Image.new("RGBA", (512, 512))
    canvas.alpha_composite(fitted, (x, y))
    return canvas


def main() -> None:
    atlas = Image.open(ATLAS).convert("RGBA")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for row, tier in enumerate(ROWS):
        built = {}
        for direction in ("south", "east", "north"):
            column = COLUMNS.index(direction)
            image = place(source_cell(atlas, row, column), tier, direction)
            path = OUTPUT / f"ShenjiArmor_{tier}_Female_{direction}.png"
            image.save(path, optimize=True)
            built[direction] = image

        # Inventory icon: centered south armor with no additional frame.
        built["south"].save(OUTPUT / f"ShenjiArmor_{tier}.png", optimize=True)

    print(OUTPUT)


if __name__ == "__main__":
    main()
