"""Render the exact Shenji textures currently shipped by the mod.

Unlike the asset generator this script is read-only: it never rewrites game
textures.  It mirrors Milira's body/apparel/head/hair/head-apparel ordering so
directional anchor regressions can be checked before launching RimWorld.
"""

from pathlib import Path
from PIL import Image, ImageDraw


MOD = Path(__file__).resolve().parents[1]
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"
SHENJI = MOD / "Content" / "Textures" / "Yunjianzi"
OUTPUT = MOD / "ArtSource" / "Shenji" / "Shenji_CurrentAssets_QA.png"


def load(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    return image if image.size == (512, 512) else image.resize((512, 512), Image.Resampling.LANCZOS)


def checker(size: tuple[int, int]) -> Image.Image:
    canvas = Image.new("RGBA", size, (20, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, size[1], 24):
        for x in range(0, size[0], 24):
            if (x // 24 + y // 24) % 2 == 0:
                draw.rectangle((x, y, x + 23, y + 23), fill=(31, 47, 58, 255))
    return canvas


def pawn(tier: str, direction: str) -> Image.Image:
    paths = [
        REF / "Milian" / "Pawn" / "Body" / f"Naked_Female_{direction}.png",
        SHENJI / "Apparel" / "Shenji" / f"ShenjiArmor_{tier}_Female_{direction}.png",
        REF / "Milian" / "Pawn" / "Head" / f"MilianHeadN1_{direction}.png",
        SHENJI / "Pawn" / "Shenji" / f"ShenjiHairBack_{direction}.png",
        SHENJI / "Pawn" / "Shenji" / f"ShenjiHairFront_{direction}.png",
        SHENJI / "Apparel" / "Shenji" / f"ShenjiVisor_Female_{direction}.png",
    ]
    result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    for path in paths:
        result = Image.alpha_composite(result, load(path))
    return result


def main() -> None:
    tiers = ("Base", "Intermediate", "Final")
    directions = ("south", "east", "north")
    tile = 340
    sheet = checker((tile * 3, tile * 3 + 44))
    draw = ImageDraw.Draw(sheet)
    for row, tier in enumerate(tiers):
        for column, direction in enumerate(directions):
            image = pawn(tier, direction).crop((105, 115, 407, 512))
            image.thumbnail((300, 310), Image.Resampling.LANCZOS)
            x = column * tile + (tile - image.width) // 2
            y = row * tile + 4
            sheet.alpha_composite(image, (x, y))
            draw.text((column * tile + 10, row * tile + 316), f"{tier} / {direction}", fill=(210, 244, 242, 255))
    draw.text((14, tile * 3 + 14), "Current shipped layers: body -> realm armour -> head -> hair -> independent visor", fill=(132, 225, 225, 255))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
