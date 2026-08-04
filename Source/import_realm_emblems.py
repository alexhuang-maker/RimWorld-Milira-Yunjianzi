"""Import the generated five-realm emblem strip into game-ready UI textures.

The source remains archived under ArtSource.  Each fifth is alpha-cropped,
normalized to a square 128 px canvas, and exported independently so the C# UI
can tint and scale every cultivation realm without sharing one marker sprite.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/GeneratedAssets/RealmEmblems/Yunjianzi_RealmEmblems_v1_transparent.png"
OUTPUT = ROOT / "Content/Textures/Yunjianzi/UI/Realms"
PANEL = ROOT / "Content/Textures/Yunjianzi/UI/CultivationPanel.png"
PREVIEW = ROOT / "ArtSource/GeneratedAssets/RealmEmblems/Yunjianzi_RealmEmblems_InGamePreview_v1.png"


def import_emblems() -> list[Image.Image]:
    source = Image.open(SOURCE).convert("RGBA")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    emblems: list[Image.Image] = []
    for index in range(5):
        left = round(source.width * index / 5)
        right = round(source.width * (index + 1) / 5)
        cell = source.crop((left, 0, right, source.height))
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise RuntimeError(f"Realm emblem {index + 1} contains no visible pixels")
        glyph = cell.crop(bounds)
        glyph.thumbnail((116, 116), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (128, 128))
        canvas.alpha_composite(glyph, ((128 - glyph.width) // 2, (128 - glyph.height) // 2))
        canvas.save(OUTPUT / f"Realm_{index + 1}.png", optimize=True)
        emblems.append(canvas)
    return emblems


def render_preview(emblems: list[Image.Image]) -> None:
    panel = Image.open(PANEL).convert("RGBA").resize((750, 240), Image.Resampling.LANCZOS)
    # Preview the exact in-game layout at 3x scale for readable QA.  Panel v2
    # has no baked placeholders, so the emblems require no masking backplates.
    centers = [406, 458, 510, 562, 614]
    sizes = [30, 35, 39, 44, 48]
    center_y = 190
    for center, size, emblem in zip(centers, sizes, emblems):
        icon = emblem.resize((size, size), Image.Resampling.LANCZOS)
        panel.alpha_composite(icon, (center - size // 2, center_y - size // 2))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    panel.save(PREVIEW, optimize=True)


if __name__ == "__main__":
    imported = import_emblems()
    render_preview(imported)
    print(PREVIEW)
