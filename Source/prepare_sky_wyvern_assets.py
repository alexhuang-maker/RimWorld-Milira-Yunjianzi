from pathlib import Path
from collections import deque
from PIL import Image

GROUND_SOURCE = Path(r"C:\Users\alex\.codex\generated_images\019fc1fd-4fb5-7700-8fe1-53b8f7bf3ae2\exec-ff5a1243-f4d2-4857-acdb-4123c2f21e43.png")
FLYING_SOURCE = Path(r"C:\Users\alex\.codex\generated_images\019fc1fd-4fb5-7700-8fe1-53b8f7bf3ae2\exec-3ef61e4f-1d53-4028-aaf3-492c79d93f57.png")
ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "SkyEngineWyvern"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "SkyWyvern"

GROUND_ROWS = ("PurpleGround", "PurpleGroundAlt", "GoldenGround", "GoldenGroundAlt", "SpiritGround", "SpiritGroundAlt")
FLYING_ROWS = ("PurpleFlying", "PurpleFlyingAlt", "GoldenFlying", "GoldenFlyingAlt", "SpiritFlying", "SpiritFlyingAlt")
DIRECTIONS = ("south", "east", "north", "west")


def remove_magenta(image: Image.Image) -> Image.Image:
    result = image.convert("RGBA")
    pixels = result.load()
    w, h = result.size
    # Flood only from the canvas edges.  This removes the connected chroma field
    # without erasing the creature's deliberately purple lightning and wing glow.
    background = (247, 6, 225)
    queue = deque()
    seen = set()
    for x in range(w):
        queue.append((x, 0)); queue.append((x, h - 1))
    for y in range(h):
        queue.append((0, y)); queue.append((w - 1, y))

    def close_to_chroma(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        distance = ((r - background[0]) ** 2 + (g - background[1]) ** 2 + (b - background[2]) ** 2) ** .5
        return distance < 105 and r > 155 and b > 135 and g < 125

    while queue:
        x, y = queue.popleft()
        if (x, y) in seen or not close_to_chroma(x, y):
            continue
        seen.add((x, y))
        pixels[x, y] = (255, 255, 255, 0)
        if x: queue.append((x - 1, y))
        if x + 1 < w: queue.append((x + 1, y))
        if y: queue.append((x, y - 1))
        if y + 1 < h: queue.append((x, y + 1))
    # Clear enclosed holes (between limbs, wing feathers and tail coils) that the
    # edge flood cannot reach.  The threshold is deliberately limited to the flat,
    # low-green chroma family; lavender scales and cyan/purple qi retain more green.
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a and r > 145 and b > 155 and g < min(r, b) * .38 and abs(r - b) < 90:
                pixels[x, y] = (255, 255, 255, 0)
            elif a and r > 105 and b > 130 and g < max(r, b) * .62:
                # Image generation tends to reflect the magenta key into rim light.
                # Convert that surviving rim/lightning hue to Yun Jianzi's cyan qi.
                strength = max(r, b)
                pixels[x, y] = (min(185, g + 72), min(242, g + 132), min(255, strength + 28), a)
    return result


def main() -> None:
    ART.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    for source, prefix, rows in (
        (GROUND_SOURCE, "GroundWalk", GROUND_ROWS),
        (FLYING_SOURCE, "FlyingWingbeat", FLYING_ROWS),
    ):
        atlas = Image.open(source).convert("RGBA")
        if atlas.size != (1024, 1536):
            raise ValueError(f"Expected 1024x1536 atlas, got {atlas.size}")
        atlas.save(ART / f"SkyEngineWyvern_{prefix}_4x6_chroma.png")
        remove_magenta(atlas).save(ART / f"SkyEngineWyvern_{prefix}_4x6_transparent.png")
        for row, name in enumerate(rows):
            for column, direction in enumerate(DIRECTIONS):
                crop = atlas.crop((column * 256, row * 256, (column + 1) * 256, (row + 1) * 256))
                remove_magenta(crop).save(OUT / f"{name}_{direction}.png")

    preview_rows = ("PurpleGround", "PurpleFlying", "GoldenGround", "GoldenFlying", "SpiritGround", "SpiritFlying")
    preview = Image.new("RGBA", (1024, 1536), (0, 0, 0, 255))
    for y in range(0, preview.height, 32):
        for x in range(0, preview.width, 32):
            color = (25, 39, 51, 255) if ((x // 32) + (y // 32)) % 2 == 0 else (36, 54, 68, 255)
            preview.paste(color, (x, y, x + 32, y + 32))
    for row, name in enumerate(preview_rows):
        for column, direction in enumerate(DIRECTIONS):
            sprite = Image.open(OUT / f"{name}_{direction}.png").convert("RGBA")
            preview.alpha_composite(sprite, (column * 256, row * 256))
    preview.save(ART / "SkyEngineWyvern_RuntimePreview.png")


if __name__ == "__main__":
    main()
