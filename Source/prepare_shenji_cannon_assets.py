from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "GeneratedAssets" / "Shenji" / "ShenjiCannon_Stages_v1_transparent.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Weapons"
QA = ROOT / "ArtSource" / "Shenji" / "ShenjiCannon_InGame_QA.png"
LOADOUT_QA = ROOT / "ArtSource" / "Shenji" / "ShenjiCannon_Loadout_QA.png"

NAMES = (
    "ShenjiCannon_Assault",
    "ShenjiCannon_Gatling",
    "ShenjiCannon_Gauss",
    "ShenjiCannon_Legendary",
)
TITLES = ("练气 · 初式", "筑基 · 连星", "紫府 · 辉光", "金丹/化神 · 传说")


def alpha_bbox(image: Image.Image):
    # Ignore the very faint fringe left by soft chroma-key matting.
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
    return alpha.getbbox()


def weapon_ranges(source: Image.Image):
    alpha = source.getchannel("A")
    ranges = []
    start = None
    for x in range(source.width):
        solid_pixels = sum(1 for value in alpha.crop((x, 0, x + 1, source.height)).getdata() if value >= 128)
        occupied = solid_pixels > 3
        if occupied and start is None:
            start = x
        elif not occupied and start is not None:
            if x - start > 24:
                ranges.append((start, x))
            start = None
    if start is not None:
        ranges.append((start, source.width))
    if len(ranges) != 4:
        raise RuntimeError(f"Expected four separated weapons, found {ranges}")
    return ranges


def normalize_weapon(cell: Image.Image) -> Image.Image:
    bbox = alpha_bbox(cell)
    if not bbox:
        raise RuntimeError("A weapon cell is empty after chroma removal")
    sprite = cell.crop(bbox)
    # Graphic_Single is rendered on a square quad.  The previous 512x256 files
    # were stretched vertically by RimWorld, making the cannon nearly as tall
    # as the pawn.  Milira's native carried weapons use a 512x512 canvas.
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    max_w, max_h = 400, 138
    scale = min(max_w / sprite.width, max_h / sprite.height)
    size = (max(1, round(sprite.width * scale)), max(1, round(sprite.height * scale)))
    sprite = sprite.resize(size, Image.Resampling.LANCZOS)
    pos = ((canvas.width - sprite.width) // 2, (canvas.height - sprite.height) // 2)
    canvas.alpha_composite(sprite, pos)
    return canvas


def checker(size, tile=24):
    image = Image.new("RGBA", size, "#17232d")
    draw = ImageDraw.Draw(image)
    colors = ("#17232d", "#22333f")
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            draw.rectangle((x, y, x + tile - 1, y + tile - 1), fill=colors[(x // tile + y // tile) % 2])
    return image


def loadout_preview(weapon: Image.Image, armor_name: str, scale: float) -> Image.Image:
    from import_shenji_generated_armor_v2 import load_layer, preview

    armor_root = ROOT / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"
    armor = load_layer(armor_root / f"ShenjiArmor_{armor_name}_Female_east.png")
    pawn_root = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "Shenji"
    front = load_layer(pawn_root / "ShenjiHairFront_east.png")
    back = load_layer(pawn_root / "ShenjiHairBack_east.png")
    pawn = preview(armor, front, back, "east")
    gun = weapon.crop(alpha_bbox(weapon))
    target_w = round(178 * scale)
    target_h = max(1, round(gun.height * target_w / gun.width))
    gun = gun.resize((target_w, target_h), Image.Resampling.LANCZOS)
    gun = gun.rotate(18, resample=Image.Resampling.BICUBIC, expand=True)
    pawn.alpha_composite(gun, (224, 292 - gun.height // 2))
    return pawn


def main():
    source = Image.open(SOURCE).convert("RGBA")
    OUT.mkdir(parents=True, exist_ok=True)
    ranges = weapon_ranges(source)
    weapons = []
    for name, (left, right) in zip(NAMES, ranges):
        left = max(0, left - 8)
        right = min(source.width, right + 8)
        weapon = normalize_weapon(source.crop((left, 0, right, source.height)))
        weapon.save(OUT / f"{name}.png")
        weapons.append(weapon)

    qa = checker((1200, 460))
    draw = ImageDraw.Draw(qa)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 25)
        small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 18)
    except OSError:
        font = ImageFont.load_default()
        small = font
    draw.text((36, 24), "神机炮 · 四阶段游戏素材验收", fill="#e9fbff", font=font)
    draw.text((36, 62), "透明 PNG / 同轴向 / 等比例游戏画布", fill="#80dce8", font=small)
    for index, (weapon, title) in enumerate(zip(weapons, TITLES)):
        x = 24 + index * 294
        draw.rounded_rectangle((x, 108, x + 270, 410), radius=8, outline="#71cbd6", width=3, fill="#111c24")
        preview = weapon.resize((256, 128), Image.Resampling.LANCZOS)
        qa.paste(preview, (x + 7, 180), preview)
        draw.text((x + 18, 128), title, fill="#f4e7b7", font=small)
        draw.text((x + 18, 360), NAMES[index], fill="#9bd9df", font=ImageFont.load_default())
    qa.save(QA)

    stage_armor = ("Base", "Base", "Intermediate", "Final")
    stage_scale = (0.82, 0.90, 1.00, 1.08)
    sheet = checker((1240, 470))
    draw = ImageDraw.Draw(sheet)
    draw.text((28, 20), "神机米莉安 · 四阶段实装比例合成", fill="#e9fbff", font=font)
    draw.text((28, 58), "固定短发 / 战术目镜 / 境界装甲 / 对应手持神机炮", fill="#80dce8", font=small)
    for index, (weapon, title, armor_name, scale) in enumerate(zip(weapons, TITLES, stage_armor, stage_scale)):
        panel_x = 16 + index * 306
        draw.rounded_rectangle((panel_x, 96, panel_x + 290, 450), radius=8, outline="#71cbd6", width=3, fill="#111c24")
        pawn = loadout_preview(weapon, armor_name, scale).crop((105, 105, 470, 510)).resize((270, 300), Image.Resampling.LANCZOS)
        sheet.alpha_composite(pawn, (panel_x + 10, 124))
        draw.text((panel_x + 18, 108), title, fill="#f4e7b7", font=small)
    sheet.save(LOADOUT_QA)


if __name__ == "__main__":
    main()
