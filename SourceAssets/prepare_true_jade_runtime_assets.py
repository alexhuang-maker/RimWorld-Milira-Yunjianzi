from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


MOD = Path(__file__).resolve().parents[1]
TEXTURES = MOD / "Content" / "Textures" / "Yunjianzi"
ART = MOD / "ArtSource" / "TrueJadeRuntime"
ART.mkdir(parents=True, exist_ok=True)


def _fit_rgba(source: Image.Image, canvas_size: tuple[int, int], margin: int) -> Image.Image:
    source = source.convert("RGBA")
    bbox = source.getchannel("A").getbbox()
    if bbox is None:
        return Image.new("RGBA", canvas_size)
    cropped = source.crop(bbox)
    max_w = canvas_size[0] - margin * 2
    max_h = canvas_size[1] - margin * 2
    scale = min(max_w / cropped.width, max_h / cropped.height)
    size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    cropped = cropped.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", canvas_size)
    out.alpha_composite(cropped, ((canvas_size[0] - size[0]) // 2, (canvas_size[1] - size[1]) // 2))
    return out


def _door_icon_to_single_leaf(icon_path: Path, out_path: Path, canvas: tuple[int, int]) -> None:
    icon = Image.open(icon_path).convert("RGBA")
    bbox = icon.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError(f"No visible pixels in {icon_path}")
    cropped = icon.crop(bbox)
    # DrawMovers draws this texture twice and mirrors the second mesh. Keep only
    # the left leaf here so the closed result is one custom two-leaf door.
    target = (canvas[0] - 4, canvas[1] - 4)
    stretched = cropped.resize(target, Image.Resampling.LANCZOS)
    pair = Image.new("RGBA", canvas)
    pair.alpha_composite(stretched, (2, 2))
    out = Image.new("RGBA", canvas)
    left = pair.crop((0, 0, canvas[0] // 2 + 1, canvas[1]))
    out.alpha_composite(left, (0, 0))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path)


def make_wall() -> Image.Image:
    size = 256
    image = Image.new("RGBA", (size, size), (244, 249, 246, 255))
    draw = ImageDraw.Draw(image, "RGBA")
    # The Stuff colour supplies the final pale jade hue. There is deliberately
    # no frame or emblem per wall cell; the vanilla linked wall silhouette now
    # provides all topology.
    for y in range(size):
        shade = round(3 * math.sin(y / (size - 1) * math.pi))
        draw.line((0, y, size - 1, y), fill=(244 + shade, 249 + shade, 246 + shade, 255))
    return image


def make_gravship_hull_atlas() -> Image.Image:
    """Build the 4x4 linked-material atlas required by GravshipHull.

    Vanilla GravshipHull is Graphic_Single with linkType CornerFiller.  The
    linked wrapper does not read a 2x2 sprite sheet: MaterialAtlasPool samples
    sixteen padded cells from a 4x4 atlas, one for every NESW link bitmask.
    """
    atlas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    cell = 128
    inset = 16  # MaterialAtlasPool's 1/32 UV padding on a 512px texture.

    # Deliberately quiet wall surface.  Identity now comes from the material
    # colour and a very light exposed-edge bevel, not from a circuit printed on
    # every cell.  Connected cells therefore read as one continuous jade slab.
    surface = Image.new("RGBA", (cell, cell), (228, 239, 233, 255))
    sd = ImageDraw.Draw(surface, "RGBA")
    for y in range(cell):
        # Four-value ceramic shading is effectively a solid colour at colony
        # zoom, but keeps the wall from looking like an unlit debug rectangle.
        shade = int(4 * math.sin((y / (cell - 1)) * math.pi))
        sd.line((0, y, cell - 1, y), fill=(228 + shade, 239 + shade, 233 + shade, 255))

    for link_mask in range(16):
        tile = surface.copy()
        d = ImageDraw.Draw(tile, "RGBA")

        # Only exposed sides receive a narrow bevel. Connected sides stay open,
        # so a row becomes a single calm slab rather than separate framed tiles.
        edge_specs = (
            (1, ((inset, inset), (127 - inset, inset)), (0, 3), (0, 5)),
            (2, ((127 - inset, inset), (127 - inset, 127 - inset)), (-3, 0), (-5, 0)),
            (4, ((inset, 127 - inset), (127 - inset, 127 - inset)), (0, -3), (0, -5)),
            (8, ((inset, inset), (inset, 127 - inset)), (3, 0), (5, 0)),
        )
        for bit, outer, inner_shift, light_shift in edge_specs:
            if not (link_mask & bit):
                d.line((*outer[0], *outer[1]), fill=(174, 192, 184, 255), width=5)
                gold = tuple((p[0] + inner_shift[0], p[1] + inner_shift[1]) for p in outer)
                light = tuple((p[0] + light_shift[0], p[1] + light_shift[1]) for p in outer)
                d.line((*gold[0], *gold[1]), fill=(196, 162, 77, 255), width=1)
                d.line((*light[0], *light[1]), fill=(246, 249, 243, 255), width=1)

        # Linked wall textures must stay fully opaque; transparent scratches
        # would reveal the map background and read as cracks in game.
        tile.putalpha(255)

        # Unity UV row zero is the bottom row of the PNG.
        col = link_mask % 4
        row = 3 - link_mask // 4
        atlas.alpha_composite(tile, (col * cell, row * cell))
    return atlas


def gravship_tile_from_atlas(atlas: Image.Image, link_mask: int, out_size: int = 128) -> Image.Image:
    """Preview the exact padded region sampled by MaterialAtlasPool."""
    cell = 128
    col = link_mask % 4
    row = 3 - link_mask // 4
    x = col * cell + 16
    y = row * cell + 16
    return atlas.crop((x, y, x + 96, y + 96)).resize((out_size, out_size), Image.Resampling.LANCZOS)


def make_floor() -> Image.Image:
    """Quiet 16x16 staggered jade paving for bedroom-sized rooms.

    Terrain UVs span a section rather than one source texture per map cell.
    A 32-pixel rhythm keeps the joints visible in an ordinary room instead of
    stretching a single slab across most of the room.
    """
    size = 512
    unit = 32
    out = Image.new("RGB", (size, size), (228, 238, 232))
    d = ImageDraw.Draw(out, "RGBA")
    slab_colours = (
        (231, 240, 234),
        (225, 237, 231),
        (234, 242, 237),
        (222, 235, 229),
        (229, 239, 233),
        (226, 238, 233),
    )

    row_breaks = []
    for row in range(16):
        phase = row % 4
        if phase == 0:
            widths = (2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1)
        elif phase == 1:
            widths = (1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1)
        elif phase == 2:
            widths = (2, 2, 1, 2, 2, 1, 2, 2, 2)
        else:
            widths = (1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2)
        breaks = [0]
        for width in widths:
            breaks.append(min(size, breaks[-1] + width * unit))
            if breaks[-1] == size:
                break
        if breaks[-1] < size:
            breaks.append(size)
        row_breaks.append(tuple(breaks))

    for row, breaks in enumerate(row_breaks):
        y0 = row * unit
        y1 = y0 + unit
        for index, (x0, x1) in enumerate(zip(breaks, breaks[1:])):
            colour = slab_colours[(row * 2 + index) % len(slab_colours)]
            d.rectangle((x0, y0, x1, y1), fill=(*colour, 255))
            d.line((x0 + 2, y0 + 2, x1 - 2, y0 + 2), fill=(250, 252, 248, 85), width=1)
            d.line((x0 + 2, y1 - 2, x1 - 2, y1 - 2), fill=(160, 188, 177, 70), width=1)

    for y in range(0, size + 1, unit):
        yy = min(y, size - 1)
        d.line((0, yy, size - 1, yy), fill=(151, 181, 169, 115), width=2)
        if yy + 1 < size:
            d.line((0, yy + 1, size - 1, yy + 1), fill=(248, 250, 246, 80), width=1)

    for row, breaks in enumerate(row_breaks):
        y0 = row * unit
        y1 = min(size - 1, y0 + unit)
        for joint_index, x in enumerate(breaks[1:-1], start=1):
            d.line((x, y0 + 1, x, y1 - 1), fill=(145, 176, 164, 120), width=2)
            if row % 4 == 0 and joint_index % 3 == 1:
                d.line((x - 1, y0 + 10, x - 1, y0 + 22), fill=(193, 157, 76, 175), width=1)

    return out


def make_floor_qa() -> None:
    """Render the same 4x4 repeat at practical 32px and 64px cell sizes."""
    floor = make_floor()
    qa = Image.new("RGBA", (1024, 640), (23, 34, 40, 255))
    draw = ImageDraw.Draw(qa, "RGBA")
    draw.text((24, 18), "A / 32 px per cell", fill=(225, 241, 238, 255))
    draw.text((536, 18), "A / 64 px per cell", fill=(225, 241, 238, 255))

    small_repeat = floor.resize((128, 128), Image.Resampling.LANCZOS)
    large_repeat = floor.resize((256, 256), Image.Resampling.LANCZOS)
    for y in range(4):
        for x in range(4):
            qa.alpha_composite(small_repeat.convert("RGBA"), (24 + x * 128, 52 + y * 128))
    for y in range(2):
        for x in range(2):
            qa.alpha_composite(large_repeat.convert("RGBA"), (536 + x * 256, 52 + y * 256))
    qa.save(ART / "TrueJade_Floor_A_QA.png")


def prepare_facility(source: Path, output: Path, size: int) -> None:
    image = Image.open(source).convert("RGBA")
    fitted = _fit_rgba(image, (size, size), max(8, size // 48))
    output.parent.mkdir(parents=True, exist_ok=True)
    fitted.save(output)


def prepare_forge_rotation() -> None:
    production = TEXTURES / "Buildings" / "Production"
    originals = ART / "ForgeOriginals"
    originals.mkdir(parents=True, exist_ok=True)
    for direction in ("north", "east", "south", "west"):
        runtime = production / f"TrueJadeForge_{direction}.png"
        backup = originals / runtime.name
        if not backup.exists():
            Image.open(runtime).save(backup)

    # The original north art puts the protruding operator platform to the
    # north while interactionCellOffset points south.  Rotate the art mapping
    # 180 degrees so the visible platform and interaction cell coincide.
    mapping = {"north": "south", "south": "north", "east": "west", "west": "east"}
    for runtime_direction, source_direction in mapping.items():
        source = originals / f"TrueJadeForge_{source_direction}.png"
        Image.open(source).save(production / f"TrueJadeForge_{runtime_direction}.png")


def make_qa_preview() -> None:
    wall = Image.open(TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "Wall" / "Wall_YJZ_TrueJadeSteel.png")
    floor = Image.open(TEXTURES / "Terrain" / "TrueJadeTile.png").convert("RGBA")
    door = Image.open(TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "Doors" / "DoorSimple_Mover_YJZ_TrueJadeSteel.png")
    forge = Image.open(TEXTURES / "Buildings" / "Production" / "TrueJadeForge_north.png")
    pool = Image.open(TEXTURES / "Buildings" / "CultivationFacilities" / "SpiritConvergencePool.png")
    gate = Image.open(TEXTURES / "Buildings" / "CultivationFacilities" / "CelestialDaoGate.png")
    hull_atlas = Image.open(TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "GravshipHull_Atlas" / "GravshipHull_Atlas_YJZ_TrueJadeSteel.png")

    preview = Image.new("RGBA", (1536, 1024), (23, 34, 40, 255))
    # 4x3 actual wall cells.
    small_wall = wall.resize((128, 128), Image.Resampling.LANCZOS)
    for yy in range(3):
        for xx in range(4):
            preview.alpha_composite(small_wall, (24 + xx * 128, 24 + yy * 128))
    preview.alpha_composite(door.resize((256, 256), Image.Resampling.LANCZOS), (568, 86))
    preview.alpha_composite(floor.resize((384, 384), Image.Resampling.NEAREST), (24, 470))
    preview.alpha_composite(forge.resize((576, 192), Image.Resampling.LANCZOS), (440, 520))
    preview.alpha_composite(pool.resize((320, 320), Image.Resampling.LANCZOS), (1030, 24))
    preview.alpha_composite(gate.resize((430, 430), Image.Resampling.LANCZOS), (1030, 470))
    preview.save(ART / "TrueJade_Runtime_QA.png")

    hull_preview = Image.new("RGBA", (1024, 768), (23, 34, 40, 255))
    # A connected 6x4 gravship-room perimeter. Each cell uses the same link
    # mask the game calculates from its N/E/S/W neighbours.
    occupied = {(x, y) for x in range(6) for y in range(4) if x in (0, 5) or y in (0, 3)}
    for x, y in occupied:
        mask = 0
        if (x, y - 1) in occupied:  # screen-up is RimWorld north
            mask |= 1
        if (x + 1, y) in occupied:
            mask |= 2
        if (x, y + 1) in occupied:
            mask |= 4
        if (x - 1, y) in occupied:
            mask |= 8
        tile = gravship_tile_from_atlas(hull_atlas, mask, 128)
        hull_preview.alpha_composite(tile, (128 + x * 128, 112 + y * 128))
    # Isolated wall cell: verifies that the zero-link state is a deliberate,
    # complete technological panel rather than a broken fragment.
    isolated = gravship_tile_from_atlas(hull_atlas, 0, 112)
    hull_preview.alpha_composite(isolated, (8, 328))
    hull_preview.save(ART / "TrueJade_GravshipHull_QA.png")


def main() -> None:
    hull_path = TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "GravshipHull_Atlas" / "GravshipHull_Atlas_YJZ_TrueJadeSteel.png"
    hull_path.parent.mkdir(parents=True, exist_ok=True)
    linked_atlas = make_gravship_hull_atlas()
    linked_atlas.save(hull_path)

    structures = TEXTURES / "Buildings" / "Structures"
    structures.mkdir(parents=True, exist_ok=True)
    linked_atlas.save(structures / "TrueJadeWall_Atlas.png")
    isolated = gravship_tile_from_atlas(linked_atlas, 0, 256)
    isolated.save(structures / "TrueJadeWall_MenuIcon.png")

    # Retain a material appearance for vanilla walls, while the dedicated
    # linked wall below is the recommended build command in 青灵之玉.
    wall_path = TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "Wall" / "Wall_YJZ_TrueJadeSteel.png"
    wall_path.parent.mkdir(parents=True, exist_ok=True)
    make_wall().save(wall_path)

    doors = TEXTURES / "StuffAppearance" / "TrueJadeSteel" / "Doors"
    _door_icon_to_single_leaf(doors / "DoorSimple_MenuIcon.png", doors / "DoorSimple_Mover_YJZ_TrueJadeSteel.png", (256, 256))
    _door_icon_to_single_leaf(doors / "Autodoor_MenuIcon.png", doors / "Autodoor_Mover_YJZ_TrueJadeSteel.png", (256, 256))
    _door_icon_to_single_leaf(doors / "OrnateDoor_MenuIcon.png", doors / "OrnateDoor2x1_Mover_YJZ_TrueJadeSteel.png", (512, 256))

    make_floor().save(TEXTURES / "Terrain" / "TrueJadeTile.png")
    make_floor_qa()
    prepare_forge_rotation()

    facilities = MOD / "ArtSource" / "CultivationFacilities"
    runtime_facilities = TEXTURES / "Buildings" / "CultivationFacilities"
    prepare_facility(facilities / "SpiritConvergencePool_Transparent.png", runtime_facilities / "SpiritConvergencePool.png", 512)
    prepare_facility(facilities / "CelestialDaoGate_Transparent.png", runtime_facilities / "CelestialDaoGate.png", 768)
    make_qa_preview()


if __name__ == "__main__":
    main()
