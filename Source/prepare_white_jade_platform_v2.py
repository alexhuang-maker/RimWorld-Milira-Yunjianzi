from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(
    r"C:/Users/alex/.codex/generated_images/019ff126-617a-78b2-983a-5cc9e3a9cab3/"
    r"exec-1a40d82f-e2a2-42ee-8e9c-a8457eb5f2cb.png"
)
OUTPUT = ROOT / "Content/Textures/Yunjianzi/Buildings/Meditation/WhiteJadePlatform.png"


def remove_connected_preview_background(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    seen = bytearray(width * height)
    queue = deque()

    def background(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        return min(r, g, b) >= 218 and max(r, g, b) - min(r, g, b) <= 20

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        index = y * width + x
        if seen[index] or not background(x, y):
            continue
        seen[index] = 1
        pixels[x, y] = (0, 0, 0, 0)
        if x: queue.append((x - 1, y))
        if x + 1 < width: queue.append((x + 1, y))
        if y: queue.append((x, y - 1))
        if y + 1 < height: queue.append((x, y + 1))
    return rgba


def bleed_transparent_rgb(image: Image.Image, iterations: int = 12) -> Image.Image:
    array = np.array(image.convert("RGBA"))
    rgb = array[:, :, :3].astype(np.float32)
    alpha = array[:, :, 3]
    filled = alpha > 4
    rgb[~filled] = 0
    height, width = filled.shape

    for _ in range(iterations):
        sums = np.zeros_like(rgb)
        counts = np.zeros((height, width), dtype=np.float32)
        for dy, dx in ((-1, -1), (-1, 0), (-1, 1), (0, -1),
                       (0, 1), (1, -1), (1, 0), (1, 1)):
            sy = slice(max(0, -dy), min(height, height - dy))
            sx = slice(max(0, -dx), min(width, width - dx))
            dy_slice = slice(max(0, dy), min(height, height + dy))
            dx_slice = slice(max(0, dx), min(width, width + dx))
            neighbor = filled[sy, sx]
            sums[dy_slice, dx_slice] += rgb[sy, sx] * neighbor[:, :, None]
            counts[dy_slice, dx_slice] += neighbor
        frontier = (~filled) & (counts > 0)
        if not frontier.any():
            break
        rgb[frontier] = sums[frontier] / counts[frontier, None]
        filled[frontier] = True

    transparent = alpha == 0
    array[:, :, :3][transparent] = np.clip(rgb[transparent], 0, 255).astype(np.uint8)
    return Image.fromarray(array, "RGBA")


platform = remove_connected_preview_background(Image.open(SOURCE))
bounds = platform.getbbox()
if bounds:
    platform = platform.crop(bounds)
platform.thumbnail((940, 940), Image.Resampling.LANCZOS)
canvas = Image.new("RGBA", (1024, 1024))
canvas.alpha_composite(platform, ((1024 - platform.width) // 2, (1024 - platform.height) // 2))

# Contract only the generated preview fringe, then restore a soft one-pixel edge.
red, green, blue, alpha = canvas.split()
alpha = alpha.filter(ImageFilter.MinFilter(7)).filter(ImageFilter.GaussianBlur(.4))
canvas = Image.merge("RGBA", (red, green, blue, alpha))
bleed_transparent_rgb(canvas).save(OUTPUT, optimize=True)
