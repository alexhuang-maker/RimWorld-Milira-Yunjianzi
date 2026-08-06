"""Rebuild Yunjianzi's directional hair from the untouched layer master.

This script is intentionally deterministic: the six source cells are copied or
mirrored into the exact game texture paths, then the same PNGs are composited
into a four-direction QA sheet.  It never edits the source layer sheet.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
TEXTURES = MOD_ROOT / "Content" / "Textures" / "Yunjianzi"
MASTER = MOD_ROOT / "ArtSource" / "LayerSheets" / "Yunjianzi_HairLayers_Transparent_v2.png"
CALIBRATED_FRONT = MOD_ROOT / "ArtSource" / "LayerSheets" / "Yunjianzi_HairFront_Calibrated"
SIDE_REDRAW = MOD_ROOT / "ArtSource" / "GeneratedAssets" / "Yunjianzi_HairSide_Redraw_v3_transparent.png"
OUTPUT = MOD_ROOT / "ArtSource" / "SpriteDraft" / "Yunjianzi_PawnStructure_Final_v2.png"

MILIRA_WINGS = Path(
    r"C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3256974620"
) / "Content" / "Textures" / "Milira" / "Pawn" / "BodyAddon" / "Wings"

DIRS = ("south", "east", "north", "west")


def rgba(path: Path, size=(512, 512)) -> Image.Image:
    if not path.exists():
        return Image.new("RGBA", size)
    image = Image.open(path).convert("RGBA")
    if image.size != size:
        image = image.resize(size, Image.Resampling.LANCZOS)
    return image


def save_cell(image: Image.Image, folder: str, stem: str, direction: str) -> None:
    destination = TEXTURES / "Pawn" / folder / f"{stem}_{direction}.png"
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, optimize=True)


def extract_component(master: Image.Image, box, target_size, position) -> Image.Image:
    component = master.crop(box)
    bounds = component.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError(f"No hair pixels in source region {box}")
    component = component.crop(bounds).resize(target_size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512))
    canvas.alpha_composite(component, position)
    return canvas


def side_redraw_canvas() -> Image.Image:
    sheet = Image.open(SIDE_REDRAW).convert("RGBA")
    # The generated sheet contains a left-facing source in its left half. Use
    # that one source and mirror it for east so both rotations are guaranteed
    # to share identical geometry and attachment points.
    half = sheet.crop((0, 0, sheet.width // 2, sheet.height))
    bounds = half.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("The redrawn side-hair source is empty")
    hair = half.crop(bounds).resize((180, 332), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512))
    canvas.alpha_composite(hair, (195, 160))
    return canvas


def tint_south_hair_tips(image: Image.Image) -> Image.Image:
    """Strengthen the white-to-cyan fade on the two long frontal locks.

    The central fringe ends above this range and therefore remains white. Dark
    ink lines are protected so the Milira outline weight is unchanged.
    """
    result = image.copy().convert("RGBA")
    pixels = result.load()
    start_y, end_y = 318, 452
    target = (112, 211, 232)
    for y in range(start_y, min(end_y + 1, result.height)):
        vertical = max(0.0, min(1.0, (y - start_y) / (end_y - start_y)))
        strength = vertical * 0.58
        for x in range(result.width):
            r, g, b, a = pixels[x, y]
            if a < 8 or (r + g + b) / 3 < 105:
                continue
            pixels[x, y] = (
                round(r * (1 - strength) + target[0] * strength),
                round(g * (1 - strength) + target[1] * strength),
                round(b * (1 - strength) + target[2] * strength),
                a,
            )
    return result


def rebuild_hair() -> None:
    master = rgba(MASTER, (1536, 1024))
    # The master is an art-layout sheet, not a 3x2 grid of game-ready canvases.
    # South keeps the approved fringe and north keeps the tied rear silhouette.
    # East/west use one coherent profile redraw derived from the fixed Milira
    # head; mirroring that source guarantees identical skull attachment points.
    front_south = tint_south_hair_tips(rgba(CALIBRATED_FRONT / "YunjianziHairFront_south.png"))
    back_south = extract_component(master, (220, 380, 480, 990), (183, 316), (164, 160))
    back_north = extract_component(master, (1030, 380, 1290, 990), (185, 337), (163, 160))
    # The authored side sprite already frames the single native Milira ear.
    # Do not punch a second triangular alpha hole from the head silhouette: the
    # old post-process exposed the cheek/jaw as another apparent pointed ear.
    unified_west = side_redraw_canvas()
    unified_east = unified_west.transpose(Image.Transpose.FLIP_LEFT_RIGHT)

    save_cell(front_south, "HairFront", "YunjianziHairFront", "south")
    save_cell(unified_west, "HairFront", "YunjianziHairFront", "west")
    save_cell(unified_east, "HairFront", "YunjianziHairFront", "east")
    save_cell(back_north, "HairFront", "YunjianziHairFront", "north")

    # Keep a native Milira HairBG node for variant linkage. The continuous long
    # silhouette lives in HairMain; all rear sprites remain transparent so the
    # exposed MiliraHead4 ear cannot be mistaken for a hole in a second wig.
    save_cell(Image.new("RGBA", (512, 512)), "HairBack", "YunjianziHairBack", "south")
    save_cell(Image.new("RGBA", (512, 512)), "HairBack", "YunjianziHairBack", "west")
    save_cell(Image.new("RGBA", (512, 512)), "HairBack", "YunjianziHairBack", "east")
    save_cell(Image.new("RGBA", (512, 512)), "HairBack", "YunjianziHairBack", "north")

    # Preserve the complete south rear-hair source for future animation work;
    # it is deliberately not loaded by the current foreground addon.
    source_copy = MOD_ROOT / "ArtSource" / "LayerSheets" / "Yunjianzi_HairBack_south_source.png"
    back_south.save(source_copy, optimize=True)


def directional(path_stem: Path, direction: str, mirror_west=False) -> Image.Image:
    candidate = path_stem.parent / f"{path_stem.name}_{direction}.png"
    if candidate.exists():
        return rgba(candidate)
    if mirror_west and direction == "west":
        east = path_stem.parent / f"{path_stem.name}_east.png"
        return rgba(east).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return Image.new("RGBA", (512, 512))


def wing_layers(direction: str):
    behind = Image.new("RGBA", (512, 512))
    front = Image.new("RGBA", (512, 512))
    for side in ("Left", "Right"):
        behind.alpha_composite(rgba(MILIRA_WINGS / f"{side}WingBack_{direction}.png"))
        front.alpha_composite(rgba(MILIRA_WINGS / f"{side}WingFront_{direction}.png"))
    return behind, front


def pawn_layers(direction: str, include_wings: bool):
    robe = directional(TEXTURES / "Apparel" / "YunjianziRobe_Female", direction, mirror_west=True)
    head = directional(TEXTURES / "Pawn" / "Head" / "YunjianziHead", direction)
    hair_back = directional(TEXTURES / "Pawn" / "HairBack" / "YunjianziHairBack", direction)
    hair_front = directional(TEXTURES / "Pawn" / "HairFront" / "YunjianziHairFront", direction)
    halo = directional(TEXTURES / "Pawn" / "Halo" / "YunjianziHalo", direction)
    wing_back, wing_front = wing_layers(direction)

    layers = []
    if include_wings:
        layers.append(("wings behind", wing_back))
    layers.extend((("rear hair (native BG)", hair_back), ("robe", robe), ("fixed head", head), ("continuous main hair", hair_front)))
    if include_wings:
        layers.append(("wings front", wing_front))
    layers.append(("halo", halo))
    return layers


def composite(direction: str, include_wings: bool) -> Image.Image:
    result = Image.new("RGBA", (512, 512))
    for _, layer in pawn_layers(direction, include_wings):
        result.alpha_composite(layer)
    return result


def font(size: int, bold=False):
    candidates = [
        Path(r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc"),
        Path(r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def checker(size=(512, 512), cell=32):
    image = Image.new("RGBA", size, (23, 32, 42, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(31, 44, 56, 255))
    return image


def fit_preview(image: Image.Image, box=(390, 390)):
    crop_box = image.getbbox()
    if not crop_box:
        return Image.new("RGBA", box)
    cropped = image.crop(crop_box)
    cropped.thumbnail(box, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", box)
    result.alpha_composite(cropped, ((box[0] - cropped.width) // 2, (box[1] - cropped.height) // 2))
    return result


def render_sheet() -> None:
    width, height = 2060, 1580
    sheet = Image.new("RGBA", (width, height), (13, 21, 29, 255))
    draw = ImageDraw.Draw(sheet)
    title = font(48, True)
    section = font(30, True)
    label = font(24, True)
    small = font(20)

    draw.text((55, 34), "云间子 · 四向小人结构本地验收图", font=title, fill=(225, 248, 250, 255))
    draw.text((57, 94), "Actual game PNG layers / deterministic composite", font=small, fill=(116, 210, 218, 255))
    draw.text((55, 142), "无遮挡结构（检查额头、耳尖、侧发与后束发）", font=section, fill=(236, 226, 190, 255))

    names = {"south": "南 / 正面", "east": "东 / 向右", "north": "北 / 背面", "west": "西 / 向左"}
    x_positions = (55, 560, 1065, 1570)
    for direction, x in zip(DIRS, x_positions):
        panel = checker((435, 435))
        panel.alpha_composite(fit_preview(composite(direction, False), (405, 405)), (15, 15))
        sheet.alpha_composite(panel, (x, 192))
        draw.rectangle((x, 192, x + 435, 627), outline=(121, 190, 199, 255), width=3)
        draw.text((x + 12, 638), names[direction], font=label, fill=(236, 244, 246, 255))

    draw.text((55, 708), "含米莉拉翅膀的游戏层级预览", font=section, fill=(236, 226, 190, 255))
    for direction, x in zip(DIRS, x_positions):
        panel = checker((435, 435))
        panel.alpha_composite(fit_preview(composite(direction, True), (405, 405)), (15, 15))
        sheet.alpha_composite(panel, (x, 758))
        draw.rectangle((x, 758, x + 435, 1193), outline=(121, 190, 199, 255), width=3)
        draw.text((x + 12, 1204), names[direction], font=label, fill=(236, 244, 246, 255))

    draw.text((55, 1282), "固定分层顺序", font=section, fill=(236, 226, 190, 255))
    order = "原生后发 BG → 云间道袍 → 固定头脸 → 连续长发主层 → 翅膀前层 → 光环"
    draw.text((55, 1334), order, font=label, fill=(221, 240, 242, 255))
    notes = [
        "• 东/西贴图由同一侧面母图镜像生成，不再存在重复发片或方向混用。",
        "• 北面与侧面长发遵循 Milira 长发：完整轮廓位于 HairMain，保留末端珠玉发束。",
        "• HairBG 使用原生锚点且保持透明，不再叠出第二个头壳。",
        "• 此图直接读取 Content/Textures 中将被游戏加载的 PNG，不使用概念图替代。",
    ]
    for index, note in enumerate(notes):
        draw.text((57, 1388 + index * 38), note, font=small, fill=(170, 196, 201, 255))

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(OUTPUT, quality=95)


if __name__ == "__main__":
    rebuild_hair()
    render_sheet()
    print(OUTPUT)
