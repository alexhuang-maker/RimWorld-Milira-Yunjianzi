from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


MOD = Path(__file__).resolve().parents[1]
ART = MOD / "ArtSource" / "GeneratedAssets" / "Shenji"
ARMOR_ATLAS = ART / "ShenjiArmor_Atlas_v3.png"
VISOR_ATLAS = ART / "ShenjiVisor_Atlas_v2.png"
OUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"
HAIR_OUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "Shenji"
QA = MOD / "ArtSource" / "Shenji" / "ShenjiArmor_InGameLayers_QA_v2.png"
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"

DIRECTIONS = ("south", "east", "north")
TIERS = ((1, "Base"), (2, "Intermediate"), (3, "Final"))


def remove_magenta(image: Image.Image) -> Image.Image:
    """Remove generated #ff00ff and mathematically decontaminate antialiased edges."""
    rgba = np.asarray(image.convert("RGBA"), dtype=np.float32)
    rgb = rgba[..., :3]
    magenta = np.array([255.0, 0.0, 255.0], dtype=np.float32)
    distance = np.linalg.norm(rgb - magenta, axis=2)
    alpha = np.clip((distance - 12.0) / 92.0, 0.0, 1.0)
    # Image generation antialiases black outlines against magenta, producing dark
    # purple pixels that are far from #ff00ff in Euclidean distance. Recover their
    # original black color from the chroma composite instead of leaving a pink rim.
    rb_min = np.minimum(rgb[..., 0], rgb[..., 2])
    purple_matte = (np.abs(rgb[..., 0] - rgb[..., 2]) < 58) & (rgb[..., 1] < rb_min * .58)
    matte_alpha = np.clip(1.0 - rb_min / 255.0, 0.0, 1.0)
    alpha = np.where(purple_matte, matte_alpha, alpha)
    alpha = np.where((rgb[..., 0] > 210) & (rgb[..., 2] > 210) & (rgb[..., 1] < 85), 0.0, alpha)
    safe = np.maximum(alpha[..., None], 0.02)
    foreground = (rgb - (1.0 - alpha[..., None]) * magenta) / safe
    foreground = np.clip(foreground, 0.0, 255.0)
    out = np.dstack((foreground, alpha[..., None] * 255.0)).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


def extract_cell(atlas: Image.Image, row: int, col: int, rows: int, cols: int) -> Image.Image:
    width, height = atlas.size
    x0, x1 = round(col * width / cols), round((col + 1) * width / cols)
    y0, y1 = round(row * height / rows), round((row + 1) * height / rows)
    cell = remove_magenta(atlas.crop((x0, y0, x1, y1)))
    bbox = cell.getchannel("A").point(lambda p: 255 if p > 20 else 0).getbbox()
    if not bbox:
        raise RuntimeError(f"empty generated cell row={row} col={col}")
    return cell.crop(bbox)


def place_on_game_canvas(sprite: Image.Image, max_width: int, target_height: int, bottom: int, center_x: int = 256) -> Image.Image:
    ratio = min(max_width / sprite.width, target_height / sprite.height)
    size = (max(1, round(sprite.width * ratio)), max(1, round(sprite.height * ratio)))
    sprite = sprite.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    x = center_x - sprite.width // 2
    y = bottom - sprite.height
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def place_exact_on_game_canvas(sprite: Image.Image, target_width: int, target_height: int, bottom: int, center_x: int = 256) -> Image.Image:
    """Fit generated apparel to a measured Milian worn-graphic silhouette.

    The generated side cells contain much more transparent horizontal padding
    than their south/north counterparts.  A single aspect-preserving max width
    therefore produced a 96 px-wide side shell in game.  Worn graphics are
    directional sprites, so each direction needs its own measured silhouette.
    """
    sprite = sprite.resize((target_width, target_height), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    canvas.alpha_composite(sprite, (center_x - target_width // 2, bottom - target_height))
    return canvas


def fit_armor(sprite: Image.Image, stage: int, direction: str) -> Image.Image:
    # Calibrated against Milian_GuardArmor's actual 512 px worn graphics.
    # In particular the east shell must be broad enough to form a complete
    # torso/hip silhouette, rather than a narrow plate pasted onto the body.
    sizes = {
        "south": {1: (218, 304), 2: (238, 310), 3: (254, 316)},
        "east":  {1: (158, 304), 2: (172, 310), 3: (186, 316)},
        "north": {1: (220, 304), 2: (240, 310), 3: (256, 316)},
    }
    width, height = sizes[direction][stage]
    return place_exact_on_game_canvas(sprite, width, height, 472)


def build_integrated_hair(direction: str, visor: Image.Image):
    """Bake the visor into Milira's animated front-hair layer."""
    front = load_layer(REF / "Milian" / "Pawn" / "Hair" / "Milian_HairN1" / f"MiliraHair4_{direction}.png")
    back = load_layer(REF / "Milira" / "Pawn" / "Hair" / "HairBackGround" / f"MiliraHairBG4_{direction}.png")
    # The north view is the back of the head.  Drawing a front visor there
    # produced a conspicuous rectangular helmet plate; only south/east need it.
    if direction != "north":
        front = Image.alpha_composite(front, visor)
    front.save(HAIR_OUT / f"ShenjiHairFront_{direction}.png")
    back.save(HAIR_OUT / f"ShenjiHairBack_{direction}.png")
    return front, back


def fit_visor(sprite: Image.Image, direction: str) -> Image.Image:
    settings = {
        "south": (142, 76, 303, 256),
        "east": (100, 72, 302, 252),
        "north": (140, 58, 289, 256),
    }
    width, height, bottom, center = settings[direction]
    return place_on_game_canvas(sprite, width, height, bottom, center)


def load_layer(path: Path) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    return image if image.size == (512, 512) else image.resize((512, 512), Image.Resampling.LANCZOS)


def preview(armor: Image.Image, front_hair: Image.Image, back_hair: Image.Image, direction: str) -> Image.Image:
    layers = (
        armor,
        load_layer(REF / "Milian" / "Pawn" / "Head" / f"MilianHeadN1_{direction}.png"),
        back_hair,
        front_hair,
    )
    result = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    for layer in layers:
        result = Image.alpha_composite(result, layer)
    return result


def checker(size):
    image = Image.new("RGBA", size, (19, 30, 39, 255))
    draw = ImageDraw.Draw(image)
    step = 24
    for y in range(0, size[1], step):
        for x in range(0, size[0], step):
            if (x // step + y // step) % 2 == 0:
                draw.rectangle((x, y, x + step - 1, y + step - 1), fill=(31, 47, 58, 255))
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    HAIR_OUT.mkdir(parents=True, exist_ok=True)
    QA.parent.mkdir(parents=True, exist_ok=True)
    armor_atlas = Image.open(ARMOR_ATLAS).convert("RGBA")
    visor_atlas = Image.open(VISOR_ATLAS).convert("RGBA")

    # The generated armor is a complete body shell; suppress the inherited nude
    # Milian body at PawnKind life-stage level instead of trying to paint over it.
    transparent_body = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    for direction in DIRECTIONS:
        transparent_body.save(HAIR_OUT / f"ShenjiTransparentBody_{direction}.png")
    transparent_body.save(HAIR_OUT / "ShenjiTransparentBody.png")

    visors = {}
    hair = {}
    for col, direction in enumerate(DIRECTIONS):
        visor = fit_visor(extract_cell(visor_atlas, 0, col, 1, 3), direction)
        visor.save(OUT / f"ShenjiVisor_Female_{direction}.png")
        visors[direction] = visor
        hair[direction] = build_integrated_hair(direction, visor)
    visors["south"].save(OUT / "ShenjiVisor.png")
    hair["south"][0].save(HAIR_OUT / "ShenjiHairFront.png")
    hair["south"][1].save(HAIR_OUT / "ShenjiHairBack.png")

    previews = {}
    for row, (stage, name) in enumerate(TIERS):
        for col, direction in enumerate(DIRECTIONS):
            armor = fit_armor(extract_cell(armor_atlas, row, col, 3, 3), stage, direction)
            armor.save(OUT / f"ShenjiArmor_{name}_Female_{direction}.png")
            previews[(stage, direction)] = preview(armor, hair[direction][0], hair[direction][1], direction)
        Image.open(OUT / f"ShenjiArmor_{name}_Female_south.png").save(OUT / f"ShenjiArmor_{name}.png")

    tile = 310
    qa = checker((tile * 3, tile * 3 + 62))
    draw = ImageDraw.Draw(qa)
    labels = {1: "BASE / QI", 2: "INTERMEDIATE / ZIFU", 3: "FINAL / HUASHEN"}
    for row, (stage, _) in enumerate(TIERS):
        for col, direction in enumerate(DIRECTIONS):
            crop = previews[(stage, direction)].crop((95, 125, 417, 510)).resize((251, 300), Image.Resampling.LANCZOS)
            qa.alpha_composite(crop, (col * tile + 29, row * tile + 2))
            draw.text((col * tile + 10, row * tile + 284), f"{labels[stage]} / {direction.upper()}", fill=(219, 247, 245, 255))
    draw.text((16, tile * 3 + 18), "Actual layers: complete Shenji shell + Milian head + animated short hair/visor; north has no visor.", fill=(129, 226, 225, 255))
    qa.save(QA)
    print(QA)


if __name__ == "__main__":
    main()
