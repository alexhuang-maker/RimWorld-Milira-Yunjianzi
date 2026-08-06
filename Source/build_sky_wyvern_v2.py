"""Build clean four-direction, eight-frame Sky Engine Wyvern sprites."""

from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "SkyEngineWyvern"
UP = ART / "SkyEngineWyvern_Mechanical_KeyframeUp_v2.png"
DOWN = ART / "SkyEngineWyvern_Mechanical_KeyframeDown_v2.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "SkyWyvern"
QA = ART / "SkyEngineWyvern_RuntimePreview_v2.png"
FLIGHT_QA = ART / "SkyEngineWyvern_FlightFrames_QA_v2.png"

STAGES = ("Purple", "Golden", "Spirit")
DIRECTIONS = ("south", "east", "north", "west")
# Generated columns are front, screen-left, back, screen-right. RimWorld east
# faces screen-right, hence the deliberate 3/1 swap.
SOURCE_COLUMN = {"south": 0, "east": 3, "north": 2, "west": 1}
FLIGHT_ROW = {"south": 0, "east": 3, "north": 2, "west": 1}


def remove_chroma(cell: Image.Image) -> Image.Image:
    image = cell.convert("RGBA")
    px = image.load()
    width, height = image.size
    seen: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()
    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))

    def is_key(x: int, y: int) -> bool:
        r, g, b, _ = px[x, y]
        return r > 170 and b > 145 and g < 105 and (r + b) > g * 4.2

    while queue:
        x, y = queue.popleft()
        if (x, y) in seen or not is_key(x, y):
            continue
        seen.add((x, y))
        px[x, y] = (255, 255, 255, 0)
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))

    for y in range(height):
        for x in range(width):
            r, g, b, a = px[x, y]
            if a and r > 175 and b > 160 and g < 95:
                px[x, y] = (255, 255, 255, 0)
            elif a and r > 150 and b > 145 and g < 115 and (r + b) > g * 3.3:
                px[x, y] = (150, 220, 235, min(a, 185))
    return image


def keep_largest_component(image: Image.Image) -> Image.Image:
    """Discard leaked fragments from neighbouring atlas cells and loose sparks."""
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    components: list[list[tuple[int, int]]] = []
    for y in range(height):
        for x in range(width):
            if pixels[x, y] < 24 or (x, y) in visited:
                continue
            component: list[tuple[int, int]] = []
            queue = deque(((x, y),))
            visited.add((x, y))
            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for nx in range(max(0, cx - 1), min(width, cx + 2)):
                    for ny in range(max(0, cy - 1), min(height, cy + 2)):
                        if pixels[nx, ny] >= 24 and (nx, ny) not in visited:
                            visited.add((nx, ny))
                            queue.append((nx, ny))
            components.append(component)
    if not components:
        return image
    keep = set(max(components, key=len))
    result = image.copy()
    out = result.load()
    for y in range(height):
        for x in range(width):
            if (x, y) not in keep:
                r, g, b, _ = out[x, y]
                out[x, y] = (r, g, b, 0)
    return result


def split_flight_row(atlas: Image.Image, direction: str) -> list[Image.Image]:
    """Split a generated row by connected subjects, not assumed cell borders."""
    width, height = atlas.size
    row = FLIGHT_ROW[direction]
    top = round(row * height / 4)
    bottom = round((row + 1) * height / 4)
    strip = remove_chroma(atlas.crop((0, top, width, bottom)))
    alpha = strip.getchannel("A")
    pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    components: list[list[tuple[int, int]]] = []
    for y in range(strip.height):
        for x in range(strip.width):
            if pixels[x, y] < 24 or (x, y) in visited:
                continue
            component: list[tuple[int, int]] = []
            queue = deque(((x, y),))
            visited.add((x, y))
            while queue:
                cx, cy = queue.popleft()
                component.append((cx, cy))
                for nx in range(max(0, cx - 1), min(strip.width, cx + 2)):
                    for ny in range(max(0, cy - 1), min(strip.height, cy + 2)):
                        if pixels[nx, ny] >= 24 and (nx, ny) not in visited:
                            visited.add((nx, ny))
                            queue.append((nx, ny))
            if len(component) > 350:
                components.append(component)
    components = sorted(components, key=len, reverse=True)[:8]
    components.sort(key=lambda points: sum(x for x, _ in points) / len(points))
    if len(components) not in (7, 8):
        raise RuntimeError(f"Expected 7/8 animation subjects for {direction}, found {len(components)}")
    frames: list[Image.Image] = []
    source = strip.load()
    for component in components:
        xs = [x for x, _ in component]
        ys = [y for _, y in component]
        left, top_y, right, bottom_y = min(xs), min(ys), max(xs) + 1, max(ys) + 1
        isolated = Image.new("RGBA", (right - left, bottom_y - top_y), (0, 0, 0, 0))
        target = isolated.load()
        for x, y in component:
            target[x - left, y - top_y] = source[x, y]
        frames.append(normalize(isolated))
    # A closed animation conventionally repeats the upstroke at the loop seam.
    # Some generated atlases contain seven distinct wing positions; duplicating
    # the first pose as frame eight keeps eight files without inventing a ghost.
    if len(frames) == 7:
        frames.append(frames[0].copy())
    return frames


def normalize(isolated: Image.Image) -> Image.Image:
    bbox = isolated.getchannel("A").getbbox()
    if bbox is None:
        return Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    subject = isolated.crop(bbox)
    scale = min(232 / subject.width, 232 / subject.height, 1.0)
    subject = subject.resize(
        (max(1, round(subject.width * scale)), max(1, round(subject.height * scale))),
        Image.Resampling.LANCZOS,
    )
    result = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    result.alpha_composite(subject, ((256 - subject.width) // 2, (256 - subject.height) // 2))
    return result


def crop(atlas: Image.Image, row: int, direction: str) -> Image.Image:
    column = SOURCE_COLUMN[direction]
    # Use an expanded crop so long tails are not chopped at the nominal grid
    # line. Neighbouring sprites are removed by connected-component selection.
    center_x = column * 256 + 128
    center_y = row * 256 + 128
    padded = Image.new("RGBA", (320, 320), (255, 0, 255, 255))
    left, top = center_x - 160, center_y - 160
    source_box = (max(0, left), max(0, top), min(atlas.width, left + 320), min(atlas.height, top + 320))
    region = atlas.crop(source_box)
    padded.alpha_composite(region, (max(0, -left), max(0, -top)))
    return normalize(keep_largest_component(remove_chroma(padded)))


def main() -> None:
    up_atlas = Image.open(UP).convert("RGBA")
    down_atlas = Image.open(DOWN).convert("RGBA")
    if up_atlas.size != (1024, 1536) or down_atlas.size != (1024, 1536):
        raise ValueError("Sky Engine Wyvern atlases must be 1024x1536")
    OUT.mkdir(parents=True, exist_ok=True)

    flight_atlases = {
        stage: Image.open(ART / f"{stage}_Flight8x4_v2.png").convert("RGBA")
        for stage in STAGES
    }
    preview = Image.new("RGBA", (1024, 1536), (20, 31, 39, 255))
    draw = ImageDraw.Draw(preview)
    for y in range(0, preview.height, 32):
        for x in range(0, preview.width, 32):
            if (x // 32 + y // 32) % 2 == 0:
                draw.rectangle((x, y, x + 31, y + 31), fill=(31, 47, 58, 255))

    for stage_index, stage in enumerate(STAGES):
        ground_row = stage_index * 2
        flying_row = ground_row + 1
        for direction_index, direction in enumerate(DIRECTIONS):
            animation = split_flight_row(flight_atlases[stage], direction)
            ground = crop(up_atlas, ground_row, direction)
            flying_up = crop(up_atlas, flying_row, direction)
            flying_down = crop(down_atlas, flying_row, direction)
            ground.save(OUT / f"{stage}Ground_{direction}.png")
            ground.save(OUT / f"{stage}GroundAlt_{direction}.png")
            flying_up.save(OUT / f"{stage}Flying_{direction}.png")
            flying_down.save(OUT / f"{stage}FlyingAlt_{direction}.png")
            for frame in range(8):
                animation[frame].save(
                    OUT / f"{stage}FlyingFrame{frame}_{direction}.png"
                )
            preview.alpha_composite(ground, (direction_index * 256, stage_index * 512))
            preview.alpha_composite(flying_up, (direction_index * 256, stage_index * 512 + 256))

    preview.save(QA)
    flight_preview = Image.new("RGBA", (256 * 8, 256 * 3), (20, 31, 39, 255))
    flight_draw = ImageDraw.Draw(flight_preview)
    for y in range(0, flight_preview.height, 32):
        for x in range(0, flight_preview.width, 32):
            if (x // 32 + y // 32) % 2 == 0:
                flight_draw.rectangle((x, y, x + 31, y + 31), fill=(31, 47, 58, 255))
    for row, stage in enumerate(STAGES):
        for frame in range(8):
            sprite = Image.open(OUT / f"{stage}FlyingFrame{frame}_east.png").convert("RGBA")
            flight_preview.alpha_composite(sprite, (frame * 256, row * 256))
    flight_preview.save(FLIGHT_QA)
    print(QA)


if __name__ == "__main__":
    main()
