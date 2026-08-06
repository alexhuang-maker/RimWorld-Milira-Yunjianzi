"""Build the single-form four-direction, eight-frame ground-dragon atlas."""

from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV3"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "SkyWyvern"
TURNAROUND = ART / "GroundDragon_Turnaround_v3.png"
WALK = {
    "east": ART / "GroundDragon_WalkEast8_v3.png",
    "south": ART / "GroundDragon_WalkSouth8_v3.png",
    "north": ART / "GroundDragon_WalkNorth8_v3.png",
}
DIRECTIONS = ("south", "east", "north", "west")


def remove_magenta(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            magenta = min(r, b) - g
            if r > 150 and b > 145 and magenta > 45:
                pixels[x, y] = (255, 255, 255, 0)
            elif r > 125 and b > 120 and magenta > 25:
                # Preserve antialiased contours while removing the pink fringe.
                alpha = max(0, min(a, round(255 * (45 - magenta) / 20)))
                pixels[x, y] = (min(r, 210), max(g, 170), min(b, 220), alpha)
    return image


def keep_largest(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    px = alpha.load()
    seen: set[tuple[int, int]] = set()
    groups: list[list[tuple[int, int]]] = []
    for y in range(image.height):
        for x in range(image.width):
            if px[x, y] < 24 or (x, y) in seen:
                continue
            group: list[tuple[int, int]] = []
            queue = deque(((x, y),))
            seen.add((x, y))
            while queue:
                cx, cy = queue.popleft()
                group.append((cx, cy))
                for nx in range(max(0, cx - 1), min(image.width, cx + 2)):
                    for ny in range(max(0, cy - 1), min(image.height, cy + 2)):
                        if px[nx, ny] >= 24 and (nx, ny) not in seen:
                            seen.add((nx, ny))
                            queue.append((nx, ny))
            groups.append(group)
    if not groups:
        raise RuntimeError("No sprite component found")
    keep = set(max(groups, key=len))
    result = image.copy()
    out = result.load()
    for y in range(image.height):
        for x in range(image.width):
            if (x, y) not in keep:
                r, g, b, _ = out[x, y]
                out[x, y] = (r, g, b, 0)
    return result


def normalize(image: Image.Image) -> Image.Image:
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError("Sprite became transparent")
    subject = image.crop(bbox)
    scale = min(456 / subject.width, 456 / subject.height)
    size = (max(1, round(subject.width * scale)), max(1, round(subject.height * scale)))
    subject = subject.resize(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    # Keep every direction on the same ground baseline; only horizontal extent varies.
    canvas.alpha_composite(subject, ((512 - subject.width) // 2, 484 - subject.height))
    return canvas


def cell(atlas: Image.Image, index: int, count: int) -> Image.Image:
    left = round(index * atlas.width / count)
    right = round((index + 1) * atlas.width / count)
    return normalize(keep_largest(remove_magenta(atlas.crop((left, 0, right, atlas.height)))))


def checker(width: int, height: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (20, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, height, 32):
        for x in range(0, width, 32):
            if (x // 32 + y // 32) % 2 == 0:
                draw.rectangle((x, y, x + 31, y + 31), fill=(31, 47, 58, 255))
    return canvas


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    turnaround = Image.open(TURNAROUND).convert("RGBA")
    static: dict[str, Image.Image] = {}
    for index, direction in enumerate(DIRECTIONS):
        static[direction] = cell(turnaround, index, 4)
        static[direction].save(OUT / f"UnifiedGround_{direction}.png")

    frames: dict[str, list[Image.Image]] = {}
    for direction in ("south", "east", "north"):
        atlas = Image.open(WALK[direction]).convert("RGBA")
        frames[direction] = [cell(atlas, index, 8) for index in range(8)]
    frames["west"] = [ImageOps.mirror(frame) for frame in frames["east"]]
    for direction in DIRECTIONS:
        for index, frame in enumerate(frames[direction]):
            frame.save(OUT / f"UnifiedWalkFrame{index}_{direction}.png")

    qa = checker(2048, 1024)
    for column, direction in enumerate(DIRECTIONS):
        qa.alpha_composite(static[direction], (column * 512, 0))
    for index, frame in enumerate(frames["east"]):
        thumb = frame.resize((256, 256), Image.Resampling.LANCZOS)
        qa.alpha_composite(thumb, (index * 256, 512))
    for index, frame in enumerate(frames["north"]):
        thumb = frame.resize((256, 256), Image.Resampling.LANCZOS)
        qa.alpha_composite(thumb, (index * 256, 768))
    qa.save(ART / "GroundDragon_Runtime_QA_v3.png")

    gif_frames = [frame.resize((384, 384), Image.Resampling.LANCZOS) for frame in frames["east"]]
    gif_frames[0].save(
        ART / "GroundDragon_Walk_East_v3.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=110,
        loop=0,
        disposal=2,
    )
    print(ART / "GroundDragon_Runtime_QA_v3.png")


if __name__ == "__main__":
    main()
