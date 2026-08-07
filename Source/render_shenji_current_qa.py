"""Render the exact Shenji textures currently shipped by the mod.

Unlike the asset generator this script is read-only: it never rewrites game
textures.  It mirrors Milira's body/apparel/head/hair/head-apparel ordering so
directional anchor regressions can be checked before launching RimWorld.
"""

from pathlib import Path
from PIL import Image, ImageDraw, ImageOps


MOD = Path(__file__).resolve().parents[1]
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"
SHENJI = MOD / "Content" / "Textures" / "Yunjianzi"
OUTPUT = MOD / "ArtSource" / "Shenji" / "Shenji_CurrentAssets_QA.png"
EQUIPMENT_OUTPUT = MOD / "ArtSource" / "Shenji" / "Shenji_ThreeTier_Equipment_QA.png"


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
    source_direction = "east" if direction == "west" else direction
    common_body = REF / "Milian" / "Pawn" / "Body" / f"Naked_Female_{source_direction}.png"
    common_head = REF / "Milian" / "Pawn" / "Head" / f"MilianHeadN1_{source_direction}.png"
    common_hair = REF / "Milian" / "Pawn" / "Hair" / "Milian_HairN1" / f"MiliraHair4_{source_direction}.png"
    if tier == "GuardReference":
        paths = [
            common_body,
            REF / "Milian" / "Apparel" / "Middle" / "GuardArmor" / f"GuardArmor_Female_{source_direction}.png",
            common_head,
            common_hair,
            REF / "Milian" / "Apparel" / "OverHead" / "Goggle" / f"Goggle_{source_direction}.png",
        ]
    else:
        paths = [
            common_body,
            SHENJI / "Apparel" / "Shenji" / f"ShenjiArmor_{tier}_Female_{source_direction}.png",
            common_head,
            SHENJI / "Pawn" / "Shenji" / f"ShenjiHairBack_{source_direction}.png",
            SHENJI / "Pawn" / "Shenji" / f"ShenjiHairFront_{source_direction}.png",
            SHENJI / "Apparel" / "Shenji" / f"ShenjiVisor_Female_{source_direction}.png",
        ]
    result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    for path in paths:
        result = Image.alpha_composite(result, load(path))
    return ImageOps.mirror(result) if direction == "west" else result


def main() -> None:
    tiers = ("GuardReference", "Base", "Intermediate", "Final")
    directions = ("south", "east", "west", "north")
    tile = 300
    sheet = checker((tile * 4, tile * 4 + 44))
    draw = ImageDraw.Draw(sheet)
    for row, tier in enumerate(tiers):
        for column, direction in enumerate(directions):
            # Preserve the full 512 canvas so the cyan x=256 guide measures the
            # actual RimWorld anchor instead of recentering each visible bbox.
            image = pawn(tier, direction).resize((280, 280), Image.Resampling.LANCZOS)
            x = column * tile + 10
            y = row * tile + 4
            sheet.alpha_composite(image, (x, y))
            guide_x = x + 140
            draw.line((guide_x, y, guide_x, y + 280), fill=(40, 220, 220, 105), width=1)
            draw.text((column * tile + 10, row * tile + 282), f"{tier} / {direction}", fill=(210, 244, 242, 255))
    draw.text((14, tile * 4 + 14), "Row 1 is native Milira Guard. Rows 2-4 preserve its exact goggle silhouette and anchor.", fill=(132, 225, 225, 255))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT)
    render_equipment_sheet()
    print(OUTPUT)
    print(EQUIPMENT_OUTPUT)


def fit_visible(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    result = Image.new("RGBA", size)
    if bounds is None:
        return result
    visible = image.crop(bounds)
    visible.thumbnail((size[0] - 24, size[1] - 42), Image.Resampling.LANCZOS)
    result.alpha_composite(visible, ((size[0] - visible.width) // 2, (size[1] - visible.height) // 2))
    return result


def render_equipment_sheet() -> None:
    tiers = (
        ("Base - Qigong / Foundation", "Base", "ShenjiCannon_Assault.png", "Assault / Gatling physical rounds"),
        ("Intermediate - Zifu / Jindan", "Intermediate", "ShenjiCannon_Gauss.png", "Pale-cyan beam weapon"),
        ("Final - Huashen", "Final", "ShenjiCannon_Legendary.png", "Legendary beam + ricochet node"),
    )
    directions = ("south", "east", "west", "north")
    tile_w, tile_h = 340, 330
    header = 58
    canvas = checker((tile_w * 5, header + tile_h * 3))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((0, 0, canvas.width, header), fill=(10, 24, 31, 255))
    draw.text((18, 17), "Shenji Milian - actual shipped three-tier equipment", fill=(218, 248, 245, 255))
    for row, (label, tier, weapon_name, weapon_desc) in enumerate(tiers):
        y = header + row * tile_h
        for column, direction in enumerate(directions):
            x = column * tile_w
            actual = pawn(tier, direction)
            canvas.alpha_composite(actual.resize((320, 320), Image.Resampling.LANCZOS), (x + 10, y))
            draw.line((x + tile_w // 2, y, x + tile_w // 2, y + 300), fill=(40, 220, 220, 100), width=1)
            draw.text((x + 10, y + 302), f"{label} / {direction}", fill=(215, 240, 240, 255))
        weapon_path = SHENJI / "Weapons" / weapon_name
        weapon = fit_visible(load(weapon_path), (tile_w, 255))
        wx = tile_w * 4
        canvas.alpha_composite(weapon, (wx, y + 18))
        draw.text((wx + 10, y + 274), label, fill=(240, 220, 150, 255))
        draw.text((wx + 10, y + 302), weapon_desc, fill=(165, 220, 225, 255))
    EQUIPMENT_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(EQUIPMENT_OUTPUT)


if __name__ == "__main__":
    main()
