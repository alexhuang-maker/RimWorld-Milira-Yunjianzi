"""Build Yunjianzi's complete static + animated Milira wing set.

The special-Milira renderer requires sixteen static left/right front/behind
textures and twenty-four flight frames.  We preserve the proven Milira frame
geometry and convert the reference tip accents to Yunjianzi's jade-cyan/azure
palette, so no frame can drift in size, pivot or silhouette.
"""

from __future__ import annotations

import colorsys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = MOD_ROOT.parent
REFERENCE = (
    WORKSPACE
    / "参考Mod"
    / "可选兼容"
    / "Milira Imperium [3588393755]"
    / "Content"
    / "Textures"
    / "MiliraImperium"
    / "SpecialMiliraPawn"
)
TEXTURES = MOD_ROOT / "Content" / "Textures" / "Yunjianzi"
WING_ROOT = TEXTURES / "Pawn" / "Wings"
PREVIEW_ROOT = MOD_ROOT / "ArtSource" / "GeneratedAssets"
DIRS = ("south", "east", "north", "west")


def recolor_tip_accents(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
            # Linhe's special-wing sprites use lavender only as the feather-tip
            # mask.  Neutral whites and black line art are deliberately kept.
            if v > 0.10 and s > 0.018 and 0.60 <= h <= 0.92:
                progress = min(1.0, max(0.0, (h - 0.62) / 0.22))
                target_h = 0.48 + 0.075 * progress  # jade cyan -> pale azure
                target_s = min(0.58, max(0.10, s * 1.10))
                nr, ng, nb = colorsys.hsv_to_rgb(target_h, target_s, v)
                pixels[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255), a)
    return image


def build_assets() -> None:
    for side in ("Left", "Right"):
        source_dir = REFERENCE / f"{side}WingNew_Linhe"
        target_dir = WING_ROOT / f"{side}WingNew"
        target_dir.mkdir(parents=True, exist_ok=True)
        for source in sorted(source_dir.glob("*.png")):
            recolor_tip_accents(source).save(target_dir / source.name, optimize=True)

    source_flight = REFERENCE / "Wings_Linhe"
    target_flight = WING_ROOT / "Flight"
    target_flight.mkdir(parents=True, exist_ok=True)
    for source in sorted(source_flight.glob("Milira_Fly*.png")):
        recolor_tip_accents(source).save(target_flight / source.name, optimize=True)


def rgba(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def directional(stem: Path, direction: str, mirror_west: bool = False) -> Image.Image:
    candidate = stem.parent / f"{stem.name}_{direction}.png"
    if candidate.exists():
        return rgba(candidate)
    if mirror_west and direction == "west":
        east = stem.parent / f"{stem.name}_east.png"
        return rgba(east).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return Image.new("RGBA", (512, 512))


def checker(size: tuple[int, int], cell: int = 24) -> Image.Image:
    image = Image.new("RGBA", size, (18, 29, 38, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(27, 42, 52, 255))
    return image


def font(size: int, bold: bool = False):
    candidates = (
        Path(r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc"),
        Path(r"C:\Windows\Fonts\segoeuib.ttf" if bold else r"C:\Windows\Fonts\segoeui.ttf"),
    )
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def fit(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    box = image.getbbox()
    if not box:
        return Image.new("RGBA", size)
    crop = image.crop(box)
    crop.thumbnail(size, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", size)
    result.alpha_composite(crop, ((size[0] - crop.width) // 2, (size[1] - crop.height) // 2))
    return result


def static_wings(direction: str) -> tuple[Image.Image, Image.Image]:
    behind = Image.new("RGBA", (512, 512))
    front = Image.new("RGBA", (512, 512))
    for side in ("Left", "Right"):
        root = WING_ROOT / f"{side}WingNew"
        behind.alpha_composite(rgba(root / f"{side}WingBehind_{direction}.png"))
        front.alpha_composite(rgba(root / f"{side}WingFront_{direction}.png"))
    return behind, front


def pawn_with_wings(direction: str) -> Image.Image:
    behind, front = static_wings(direction)
    layers = (
        behind,
        directional(TEXTURES / "Pawn" / "HairBack" / "YunjianziHairBack", direction),
        directional(TEXTURES / "Apparel" / "YunjianziRobe_Female", direction, True),
        front,
        directional(TEXTURES / "Pawn" / "Head" / "YunjianziHead", direction),
        directional(TEXTURES / "Pawn" / "HairFront" / "YunjianziHairFront", direction),
        directional(TEXTURES / "Pawn" / "Halo" / "YunjianziHalo", direction),
    )
    result = Image.new("RGBA", (512, 512))
    for layer in layers:
        result.alpha_composite(layer)
    return result


def render_static_qa() -> None:
    PREVIEW_ROOT.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (1840, 650), (11, 21, 29, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((36, 24), "云间子专属青蓝羽翼 · 四向静态渲染验收", font=font(36, True), fill=(224, 248, 248, 255))
    labels = {"south": "南 / 正面", "east": "东 / 向右", "north": "北 / 背面", "west": "西 / 向左"}
    for index, direction in enumerate(DIRS):
        x = 35 + index * 450
        panel = checker((420, 500))
        panel.alpha_composite(fit(pawn_with_wings(direction), (400, 460)), (10, 15))
        sheet.alpha_composite(panel, (x, 90))
        draw.rectangle((x, 90, x + 420, 590), outline=(103, 205, 213, 255), width=3)
        draw.text((x + 12, 603), labels[direction], font=font(22, True), fill=(235, 242, 243, 255))
    sheet.save(PREVIEW_ROOT / "Yunjianzi_Wings_Static_QA.png", optimize=True)


def flight_frame(direction: str, number: int, size: tuple[int, int]) -> Image.Image:
    source = rgba(WING_ROOT / "Flight" / f"Milira_Fly{direction}_{number}.png")
    return fit(source, size)


def render_flight_qa() -> None:
    directions = ("East", "North", "South")
    contact = Image.new("RGBA", (1640, 860), (11, 21, 29, 255))
    draw = ImageDraw.Draw(contact)
    draw.text((30, 20), "云间子专属青蓝羽翼 · 24 帧扑翼动画验收", font=font(34, True), fill=(224, 248, 248, 255))
    for row, direction in enumerate(directions):
        draw.text((28, 100 + row * 245), direction, font=font(22, True), fill=(120, 219, 226, 255))
        for frame in range(1, 9):
            x, y = 115 + (frame - 1) * 190, 82 + row * 245
            panel = checker((180, 220), 18)
            panel.alpha_composite(flight_frame(direction, frame, (170, 205)), (5, 7))
            contact.alpha_composite(panel, (x, y))
            draw.text((x + 76, y + 194), str(frame), font=font(17, True), fill=(230, 239, 240, 255))
    contact.save(PREVIEW_ROOT / "Yunjianzi_Wings_FlightFrames_QA.png", optimize=True)

    gif_frames = []
    for frame in range(1, 9):
        canvas = checker((960, 330), 24)
        for column, direction in enumerate(directions):
            canvas.alpha_composite(flight_frame(direction, frame, (300, 300)), (10 + column * 320, 15))
        gif_frames.append(canvas.convert("P", palette=Image.Palette.ADAPTIVE))
    gif_frames[0].save(
        PREVIEW_ROOT / "Yunjianzi_Wings_Flight_QA.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=120,
        loop=0,
        disposal=2,
    )


def validate() -> None:
    static_files = list(WING_ROOT.glob("*WingNew/*.png"))
    flight_files = list((WING_ROOT / "Flight").glob("*.png"))
    assert len(static_files) == 16, f"expected 16 static wing textures, got {len(static_files)}"
    assert len(flight_files) == 24, f"expected 24 flight textures, got {len(flight_files)}"
    for path in static_files:
        with Image.open(path) as image:
            assert image.size == (512, 512), f"static canvas mismatch: {path} {image.size}"
            assert image.mode == "RGBA", f"missing alpha: {path}"
    # Some official front/behind direction layers are intentionally empty;
    # validate the composed pair rather than rejecting a correct split layer.
    for direction in DIRS:
        behind, front = static_wings(direction)
        combined = Image.new("RGBA", (512, 512))
        combined.alpha_composite(behind)
        combined.alpha_composite(front)
        assert combined.getbbox(), f"empty composed static wings: {direction}"
    for path in flight_files:
        with Image.open(path) as image:
            assert image.size == (1024, 1024), f"flight canvas mismatch: {path} {image.size}"
            assert image.mode == "RGBA", f"missing alpha: {path}"
            assert image.getbbox(), f"empty texture: {path}"


if __name__ == "__main__":
    build_assets()
    validate()
    render_static_qa()
    render_flight_qa()
    print("Built 16 static textures, 24 flight frames, static QA, contact sheet and animated GIF.")
