"""Build the three Shenji armor tiers from the approved 3x4 orthographic atlas.

The generated atlas is only a design source.  This deterministic pass removes
the layout padding, scales every direction onto the native 512px Milian pawn
canvas, and writes the exact apparel paths loaded by RimWorld.
"""

from pathlib import Path

from PIL import Image


MOD = Path(__file__).resolve().parents[1]
ATLAS = MOD / "ArtSource" / "GeneratedAssets" / "Shenji" / "ShenjiArmor_Orthographic_Rebuild_v4_transparent.png"
OUTPUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"

ROWS = ("Base", "Intermediate", "Final")
COLUMNS = ("south", "east", "north", "west")

# Target boxes retain Milira's native head/feet baseline while allowing each
# tier to acquire a visibly different silhouette. West is engine-mirrored from
# east, so only the three normal RimWorld directional files are shipped.
TARGETS = {
    ("Base", "south"): (146, 176, 366, 468),
    ("Base", "east"): (151, 176, 337, 466),
    ("Base", "north"): (146, 176, 366, 468),
    ("Intermediate", "south"): (132, 174, 380, 468),
    ("Intermediate", "east"): (142, 174, 350, 468),
    ("Intermediate", "north"): (132, 174, 380, 468),
    ("Final", "south"): (115, 166, 397, 474),
    ("Final", "east"): (128, 166, 366, 474),
    ("Final", "north"): (115, 166, 397, 474),
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


def place(source: Image.Image, target_box: tuple[int, int, int, int]) -> Image.Image:
    x0, y0, x1, y1 = target_box
    target_w, target_h = x1 - x0, y1 - y0
    # The concept sheet uses conventional humanoid proportions. Milian pawns
    # are deliberately much wider and shorter, so preserving the source aspect
    # ratio produces the old "thin strip under a large head" regression.
    # Calibrate width and height independently to the native apparel box.
    fitted = source.resize((target_w, target_h), Image.Resampling.LANCZOS)
    x, y = x0, y0
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
            image = place(source_cell(atlas, row, column), TARGETS[(tier, direction)])
            path = OUTPUT / f"ShenjiArmor_{tier}_Female_{direction}.png"
            image.save(path, optimize=True)
            built[direction] = image

        # Inventory icon: centered south armor with no additional frame.
        built["south"].save(OUTPUT / f"ShenjiArmor_{tier}.png", optimize=True)

    print(OUTPUT)


if __name__ == "__main__":
    main()
