"""Prepare fully redrawn hair/halo candidates without touching runtime PNGs."""

from pathlib import Path
from math import sqrt
from shutil import copy2

from PIL import Image, ImageDraw, ImageFont


MOD_ROOT = Path(__file__).resolve().parents[1]
SOURCE = MOD_ROOT / "ArtSource" / "GeneratedAssets" / "Yunjianzi_ConceptRedraw_2026-08-22"
CANDIDATE = MOD_ROOT / "ArtSource" / "CandidateRuntime" / "Yunjianzi_ConceptRedraw_2026-08-22"
RUNTIME = MOD_ROOT / "Content" / "Textures" / "Yunjianzi"
MILIRA_WINGS = Path(r"C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3256974620") / "Content" / "Textures" / "Milira" / "Pawn" / "BodyAddon" / "Wings"
QA = MOD_ROOT / "ArtSource" / "SpriteDraft" / "Yunjianzi_ConceptRedraw_Candidate_QA.png"
DIRS = ("south", "east", "north", "west")


def chroma_key(image: Image.Image, cyan_effect=False) -> Image.Image:
    """Unmatte #ff00ff while preserving pale hair and cyan glow."""
    source = image.convert("RGBA")
    result = Image.new("RGBA", source.size)
    src = source.load()
    dst = result.load()
    for y in range(source.height):
        for x in range(source.width):
            r, g, b, _ = src[x, y]
            if not cyan_effect and r > 220 and b > 220 and g < 90:
                # Strong chroma spill at an antialiased hair edge is background,
                # never part of the white/cyan/gold palette.
                dst[x, y] = (255, 255, 255, 0)
                continue
            distance = sqrt((255 - r) ** 2 + g ** 2 + (255 - b) ** 2)
            if distance <= 26:
                dst[x, y] = (255, 255, 255, 0)
                continue
            alpha = max(0.0, min(1.0, (distance - 26) / 118))
            # Undo the magenta matte for clean antialiased boundaries.
            rr = round(max(0, min(255, (r - 255 * (1 - alpha)) / alpha))) if alpha else 255
            gg = round(max(0, min(255, g / alpha))) if alpha else 255
            bb = round(max(0, min(255, (b - 255 * (1 - alpha)) / alpha))) if alpha else 255
            # Image generation can bake a faint magenta reflection into the
            # foreground fringe. It is never part of this white/cyan/gold art.
            if rr > gg + 24 and bb > gg + 24:
                rr = min(rr, gg + (8 if cyan_effect else 18))
                bb = max(bb, gg)
            if cyan_effect and alpha > 0:
                # Spiritual light is cyan/blue-white, never violet.
                rr = min(rr, gg + 10)
                bb = max(bb, gg)
            dst[x, y] = (rr, gg, bb, round(alpha * 255))
    return result


def connected_object(image: Image.Image, seed) -> Image.Image:
    """Extract one complete painted object even when it crosses a 2x2 cell."""
    alpha = image.getchannel("A")
    binary = alpha.point(lambda value: 255 if value >= 20 else 0)
    work = binary.copy()
    ImageDraw.floodfill(work, seed, 128, thresh=0)
    mask = work.point(lambda value: 255 if value == 128 else 0)
    # Restore original soft alpha inside the selected component.
    selected_alpha = Image.new("L", image.size)
    ap, mp, op = alpha.load(), mask.load(), selected_alpha.load()
    for y in range(image.height):
        for x in range(image.width):
            if mp[x, y]:
                op[x, y] = ap[x, y]
    result = image.copy()
    result.putalpha(selected_alpha)
    bounds = selected_alpha.getbbox()
    if bounds is None:
        raise RuntimeError(f"No connected art at seed {seed}")
    return result.crop(bounds)


def hair_objects(image: Image.Image):
    return {
        "south": connected_object(image, (330, 90)),
        "east": connected_object(image, (900, 90)),
        "north": connected_object(image, (340, 630)),
        "west": connected_object(image, (830, 630)),
    }


def halo_objects(image: Image.Image):
    return {
        "south": connected_object(image, (310, 110)),
        "east": connected_object(image, (900, 110)),
        "north": connected_object(image, (350, 720)),
        "west": connected_object(image, (900, 720)),
    }


def fitted(sprite: Image.Image, box, center) -> Image.Image:
    bounds = sprite.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("Candidate cell contains no visible pixels")
    cropped = sprite.crop(bounds)
    cropped.thumbnail(box, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512))
    position = (round(center[0] - cropped.width / 2), round(center[1] - cropped.height / 2))
    canvas.alpha_composite(cropped, position)
    return canvas


def save(image: Image.Image, folder: str, stem: str, direction: str) -> None:
    path = CANDIDATE / folder / f"{stem}_{direction}.png"
    path.parent.mkdir(parents=True, exist_ok=True)
    clean = image.copy().convert("RGBA")
    px = clean.load()
    for y in range(clean.height):
        for x in range(clean.width):
            r, g, b, a = px[x, y]
            if a and r > 240 and b > 240 and g < 40:
                px[x, y] = (86, 207, 232, a) if folder == "Halo" else (255, 255, 255, 0)
    clean.save(path, optimize=True)


def prepare_hair() -> None:
    keyed = chroma_key(Image.open(SOURCE / "Hair4Dir_GoldChain_chroma.png"))
    keyed.save(SOURCE / "Hair4Dir_GoldChain_transparent.png", optimize=True)
    cells = hair_objects(keyed)
    specs = {
        "south": ((224, 250), (256, 280)),
        # Side profiles need a rearward registration against RimWorld's head
        # anchor.  East faces screen-right, so rearward is screen-left; west
        # is the exact inverse.  Keep the halo on its independent anchor.
        "east": ((215, 355), (232, 323)),
        "north": ((215, 355), (256, 323)),
        "west": ((215, 355), (280, 323)),
    }
    # RimWorld's east/west texture lookup is opposite to the profile labels
    # used on the generated art sheet. Swap the two source views at export so
    # the visible face, rear lock, forehead chain, and halo all point together.
    source_direction = {"south": "south", "east": "west", "north": "north", "west": "east"}
    for direction in DIRS:
        save(fitted(cells[source_direction[direction]], *specs[direction]), "HairFront", "YunjianziHairFront", direction)

    # Front-facing body hides the central lock. Reuse only the freshly drawn
    # north-cell lock as a new HairBG layer; no old runtime pixels are sampled.
    north = cells["north"]
    nb = north.getchannel("A").getbbox()
    if nb is None:
        raise RuntimeError("North hair cell is empty")
    tail_region = north.crop((round(nb[0] + (nb[2] - nb[0]) * 0.37), round(nb[1] + (nb[3] - nb[1]) * 0.57),
                              round(nb[0] + (nb[2] - nb[0]) * 0.63), nb[3]))
    south_back = fitted(tail_region, (38, 145), (256, 412))
    save(south_back, "HairBack", "YunjianziHairBack", "south")
    for direction in ("east", "north", "west"):
        save(Image.new("RGBA", (512, 512)), "HairBack", "YunjianziHairBack", direction)


def prepare_halo() -> None:
    keyed = chroma_key(Image.open(SOURCE / "Halo4Dir_chroma.png"), cyan_effect=True)
    # Re-establish the approved blue-jade hue after chroma unmatting. The
    # generated glow is the new art source; this only removes magenta spill.
    px = keyed.load()
    for y in range(keyed.height):
        for x in range(keyed.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            value = max(r, g, b)
            # Keep the body visibly blue-jade; reserve near-white for only the
            # hottest generated cores.
            t = max(0.0, min(1.0, (value - 218) / 37)) ** 2
            px[x, y] = (
                round(54 * (1 - t) + 226 * t),
                round(186 * (1 - t) + 250 * t),
                round(222 * (1 - t) + 255 * t),
                a,
            )
    keyed.save(SOURCE / "Halo4Dir_transparent.png", optimize=True)
    cells = halo_objects(keyed)
    # Character-left: image-right in south, image-left in north.
    specs = {
        "south": ((155, 190), (330, 160)),
        "east": ((135, 185), (278, 150)),
        "north": ((155, 190), (182, 160)),
        "west": ((135, 185), (234, 150)),
    }
    source_direction = {"south": "south", "east": "west", "north": "north", "west": "east"}
    for direction in DIRS:
        save(fitted(cells[source_direction[direction]], *specs[direction]), "Halo", "YunjianziHalo", direction)


def rgba(path: Path) -> Image.Image:
    if not path.exists():
        return Image.new("RGBA", (512, 512))
    image = Image.open(path).convert("RGBA")
    return image.resize((512, 512), Image.Resampling.LANCZOS) if image.size != (512, 512) else image


def runtime_directional(folder: str, stem: str, direction: str) -> Image.Image:
    path = RUNTIME / folder / f"{stem}_{direction}.png"
    if not path.exists() and direction == "west":
        east = RUNTIME / folder / f"{stem}_east.png"
        if east.exists():
            return rgba(east).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    return rgba(path)


def candidate_directional(folder: str, stem: str, direction: str) -> Image.Image:
    return rgba(CANDIDATE / folder / f"{stem}_{direction}.png")


def composite(direction: str, wings=False) -> Image.Image:
    result = Image.new("RGBA", (512, 512))
    if wings:
        for side in ("Left", "Right"):
            result.alpha_composite(rgba(MILIRA_WINGS / f"{side}WingBack_{direction}.png"))
    # Candidate back lock remains behind the robe/head.
    result.alpha_composite(candidate_directional("HairBack", "YunjianziHairBack", direction))
    result.alpha_composite(runtime_directional("Apparel", "YunjianziRobe_Female", direction))
    result.alpha_composite(runtime_directional("Pawn/Head", "YunjianziHead", direction))
    result.alpha_composite(candidate_directional("HairFront", "YunjianziHairFront", direction))
    # The QA's wing mode intentionally uses the bundled runtime lookup only if
    # a local copy exists; hair/halo validation does not depend on workshop IO.
    if wings:
        for side in ("Left", "Right"):
            result.alpha_composite(rgba(MILIRA_WINGS / f"{side}WingFront_{direction}.png"))
    result.alpha_composite(candidate_directional("Halo", "YunjianziHalo", direction))
    return result


def checker(size=(512, 512), cell=32):
    image = Image.new("RGBA", size, (23, 32, 42, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(31, 44, 56, 255))
    return image


def font(size, bold=False):
    path = Path(r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc")
    return ImageFont.truetype(str(path), size) if path.exists() else ImageFont.load_default()


def fit_preview(image: Image.Image, box=(410, 410)):
    bounds = image.getbbox()
    if not bounds:
        return Image.new("RGBA", box)
    crop = image.crop(bounds)
    crop.thumbnail(box, Image.Resampling.LANCZOS)
    output = Image.new("RGBA", box)
    output.alpha_composite(crop, ((box[0] - crop.width) // 2, (box[1] - crop.height) // 2))
    return output


def render_qa() -> None:
    sheet = Image.new("RGBA", (2060, 1370), (13, 21, 29, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((55, 34), "云间子 · 全新重绘候选四向 QA", font=font(46, True), fill=(225, 248, 250, 255))
    draw.text((57, 96), "CandidateRuntime only — runtime Content/Textures remains untouched", font=font(20), fill=(116, 210, 218, 255))
    names = {"south": "南 / 正面", "east": "东 / 向右", "north": "北 / 背面", "west": "西 / 向左"}
    for direction, x in zip(DIRS, (55, 560, 1065, 1570)):
        panel = checker((435, 435))
        panel.alpha_composite(fit_preview(composite(direction), (405, 405)), (15, 15))
        sheet.alpha_composite(panel, (x, 150))
        draw.rectangle((x, 150, x + 435, 585), outline=(121, 190, 199, 255), width=3)
        draw.text((x + 12, 600), names[direction], font=font(24, True), fill=(236, 244, 246, 255))
    draw.text((55, 665), "含米莉拉翅膀的实际图层顺序", font=font(30, True), fill=(236, 226, 190, 255))
    for direction, x in zip(DIRS, (55, 560, 1065, 1570)):
        panel = checker((435, 435))
        panel.alpha_composite(fit_preview(composite(direction, True), (405, 405)), (15, 15))
        sheet.alpha_composite(panel, (x, 715))
        draw.rectangle((x, 715, x + 435, 1150), outline=(121, 190, 199, 255), width=3)
        draw.text((x + 12, 1165), names[direction], font=font(24, True), fill=(236, 244, 246, 255))
    draw.text((55, 1260), "全新像素来源：Hair4Dir_GoldChain / Halo4Dir；旧运行时贴图仅提供锚点尺度。", font=font(22), fill=(190, 215, 220, 255))
    QA.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(QA, quality=95)


def prepare_candidates() -> None:
    prepare_hair()
    prepare_halo()
    render_qa()


def promote_candidates() -> None:
    """Copy only approved new candidate PNGs into the game runtime paths."""
    for folder, stem in (("HairFront", "YunjianziHairFront"),
                         ("HairBack", "YunjianziHairBack"),
                         ("Halo", "YunjianziHalo")):
        destination = RUNTIME / "Pawn" / folder
        destination.mkdir(parents=True, exist_ok=True)
        for direction in DIRS:
            copy2(CANDIDATE / folder / f"{stem}_{direction}.png",
                  destination / f"{stem}_{direction}.png")


if __name__ == "__main__":
    prepare_candidates()
    print(QA)
