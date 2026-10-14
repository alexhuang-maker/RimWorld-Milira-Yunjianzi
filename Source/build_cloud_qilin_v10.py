"""Build direction-specific cloud-qilin sprites without shrinking the long side tail.

Front/back use a square mesh.  Side views use a 2:1 mesh whose body anchor is
kept at the pawn origin while the tail occupies the extra horizontal space.
"""

from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV10"
SOURCE = ART / "CloudQilin_LongTailTurnaround_v10_chroma.png"
SOUTH_SOURCE = ART / "CloudQilin_LongTailSouth_v10_chroma.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "SkyWyvern"
DIRECTIONS = ("south", "east", "north", "west")


def remove_chroma(image: Image.Image) -> Image.Image:
    """Remove the generated magenta field and decontaminate antialiased edges."""
    result = image.convert("RGBA")
    pixels = result.load()
    key = result.getpixel((0, 0))[:3]
    kr, kg, kb = key
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, a = pixels[x, y]
            distance = ((r - kr) ** 2 + (g - kg) ** 2 + (b - kb) ** 2) ** 0.5
            alpha = 0 if distance < 28 else (255 if distance > 132 else round((distance - 28) / 104 * 255))
            if alpha < 255:
                matte = 1.0 - alpha / 255.0
                safe = max(alpha / 255.0, 0.03)
                r = round(max(0, min(255, (r - kr * matte) / safe)))
                g = round(max(0, min(255, (g - kg * matte) / safe)))
                b = round(max(0, min(255, (b - kb * matte) / safe)))
            pixels[x, y] = (r, g, b, min(a, alpha))
    return result


def x_runs(alpha: Image.Image) -> list[tuple[int, int]]:
    occupied = [alpha.crop((x, 0, x + 1, alpha.height)).getbbox() is not None for x in range(alpha.width)]
    runs: list[tuple[int, int]] = []
    start = None
    for x, present in enumerate(occupied + [False]):
        if present and start is None:
            start = x
        elif not present and start is not None:
            if x - start > 30:
                runs.append((start, x))
            start = None
    return runs


def keep_largest(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    source = alpha.load()
    seen: set[tuple[int, int]] = set()
    groups: list[list[tuple[int, int]]] = []
    for y in range(image.height):
        for x in range(image.width):
            if source[x, y] < 28 or (x, y) in seen:
                continue
            group: list[tuple[int, int]] = []
            queue = deque(((x, y),))
            seen.add((x, y))
            while queue:
                cx, cy = queue.popleft()
                group.append((cx, cy))
                for nx in range(max(0, cx - 1), min(image.width, cx + 2)):
                    for ny in range(max(0, cy - 1), min(image.height, cy + 2)):
                        if source[nx, ny] >= 28 and (nx, ny) not in seen:
                            seen.add((nx, ny))
                            queue.append((nx, ny))
            groups.append(group)
    keep = set(max(groups, key=len))
    result = image.copy()
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            if (x, y) not in keep:
                r, g, b, _ = pixels[x, y]
                pixels[x, y] = (r, g, b, 0)
    return result


def body_anchor_x(subject: Image.Image) -> float:
    """Find the torso/neck centre while ignoring the low trailing tail."""
    alpha = subject.getchannel("A")
    pixels = alpha.load()
    cutoff = round(subject.height * 0.78)
    weighted_x = 0.0
    total = 0.0
    for y in range(cutoff):
        for x in range(subject.width):
            weight = pixels[x, y] / 255.0
            weighted_x += x * weight
            total += weight
    return weighted_x / total if total else subject.width / 2


def normalize(image: Image.Image, side: bool) -> Image.Image:
    bbox = image.getchannel("A").point(lambda a: 255 if a >= 24 else 0).getbbox()
    if bbox is None:
        raise RuntimeError("Cloud qilin direction became transparent")
    subject = image.crop(bbox)
    scale = 456 / subject.height
    subject = subject.resize(
        (max(1, round(subject.width * scale)), 456), Image.Resampling.LANCZOS
    )
    # Leave deliberate transparent overhang around the anchored torso.  The
    # tail must fit beyond the old 512-square footprint without being clipped.
    width = 1536 if side else 768
    canvas = Image.new("RGBA", (width, 512), (0, 0, 0, 0))
    anchor = body_anchor_x(subject)
    x = max(0, min(width - subject.width, round(width / 2 - anchor)))
    canvas.alpha_composite(subject, (x, 492 - subject.height))
    return canvas


def checker(width: int, height: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (20, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, height, 32):
        for x in range(0, width, 32):
            if (x // 32 + y // 32) % 2 == 0:
                draw.rectangle((x, y, x + 31, y + 31), fill=(31, 47, 58, 255))
    return canvas


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    clean = remove_chroma(Image.open(SOURCE))
    clean.save(ART / "CloudQilin_LongTailTurnaround_v10_alpha.png", optimize=True)
    south_clean = remove_chroma(Image.open(SOUTH_SOURCE))
    south_clean.save(ART / "CloudQilin_LongTailSouth_v10_alpha.png", optimize=True)
    runs = x_runs(clean.getchannel("A").point(lambda a: 255 if a >= 28 else 0))
    if len(runs) != 4:
        raise RuntimeError(f"Expected four cloud-qilin silhouettes, found {runs}")

    sprites: dict[str, Image.Image] = {}
    for direction, (left, right) in zip(DIRECTIONS, runs):
        crop = clean.crop((max(0, left - 6), 0, min(clean.width, right + 6), clean.height))
        sprites[direction] = normalize(keep_largest(crop), direction in ("east", "west"))
    # The turnaround's first draft exposed the tail root on the front
    # centreline.  Use the corrected south art whose torso occludes the root.
    sprites["south"] = normalize(keep_largest(south_clean), False)
    for direction in DIRECTIONS:
        sprites[direction].save(OUT / f"UnifiedGround_{direction}.png", optimize=True)

    qa = checker(4608, 512)
    x = 0
    for direction in DIRECTIONS:
        qa.alpha_composite(sprites[direction], (x, 0))
        x += sprites[direction].width
    qa.save(ART / "CloudQilin_LongTail_Runtime_QA_v10.png", optimize=True)
    print(ART / "CloudQilin_LongTail_Runtime_QA_v10.png")


if __name__ == "__main__":
    main()
