from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\alex\.codex\generated_images\019fc1fd-4fb5-7700-8fe1-53b8f7bf3ae2")
OUTPUT = ROOT / "Content/Textures/Yunjianzi/Buildings/Bedroom"
OUTPUT.mkdir(parents=True, exist_ok=True)


SOURCES = {
    "SereneBed": "exec-216f35ba-b819-493f-87f2-931cb364e314.png",
    "Nightstand": "exec-b86c49ff-81f4-4caf-9bca-c07133e85809.png",
    "Chair": "exec-60a8f315-f02b-4f2e-9402-0cb1046e1844.png",
    "SmallTable": "exec-38593687-9ac0-4132-a8b4-27b5da06daa1.png",
    "Wardrobe": "exec-8993d8b5-db74-42f9-9593-a2f5f1df5a1e.png",
    "WallBookcase": "exec-e8224250-2163-4b8c-9d32-b579f65b5e4e.png",
}


def remove_magenta(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, _ = pixels[x, y]
            distance = ((r - 255) ** 2 + g ** 2 + (b - 255) ** 2) ** 0.5
            alpha = 0 if distance < 38 else (255 if distance > 145 else int((distance - 38) / 107 * 255))
            if alpha < 255:
                spill = 255 - alpha
                r = max(0, r - spill)
                b = max(0, b - spill)
            pixels[x, y] = (r, g, b, alpha)
    box = image.getbbox()
    return image.crop(box) if box else image


def on_canvas(image: Image.Image, size: tuple[int, int], occupancy: float = .90) -> Image.Image:
    canvas = Image.new("RGBA", size)
    image = image.copy()
    image.thumbnail((int(size[0] * occupancy), int(size[1] * occupancy)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return canvas


def save_single(name: str, image: Image.Image, occupancy: float = .90) -> None:
    on_canvas(image, (512, 512), occupancy).save(OUTPUT / f"{name}.png", optimize=True)


def save_multi(name: str, south: Image.Image, occupancy: float = .90) -> None:
    directions = {
        "south": south,
        "east": south.rotate(90, expand=True, resample=Image.Resampling.BICUBIC),
        "north": south.rotate(180, expand=True, resample=Image.Resampling.BICUBIC),
        "west": south.rotate(270, expand=True, resample=Image.Resampling.BICUBIC),
    }
    for direction, image in directions.items():
        on_canvas(image, (512, 512), occupancy).save(OUTPUT / f"{name}_{direction}.png", optimize=True)


clean = {name: remove_magenta(GENERATED / source) for name, source in SOURCES.items()}
save_multi("SereneBed", clean["SereneBed"], .96)
save_single("Nightstand", clean["Nightstand"], .88)
save_multi("Chair", clean["Chair"], .84)
save_single("SmallTable", clean["SmallTable"], .88)
save_multi("Wardrobe", clean["Wardrobe"], .88)
save_multi("WallBookcase", clean["WallBookcase"], .88)

# Building_Bookcase draws separate top-altitude bookend layers.  Transparent
# one-pixel sprites keep the vanilla renderer active without adding foreign art.
transparent = Image.new("RGBA", (8, 8))
transparent.save(OUTPUT / "WallBookcase_Bookend_east.png", optimize=True)
transparent.save(OUTPUT / "WallBookcase_Bookend_north.png", optimize=True)

# Contact sheet used for local scale/orientation QA.
preview = Image.new("RGBA", (1536, 1024), (15, 26, 35, 255))
draw = ImageDraw.Draw(preview)
for index, name in enumerate(SOURCES):
    image_path = OUTPUT / (f"{name}_south.png" if (OUTPUT / f"{name}_south.png").exists() else f"{name}.png")
    image = Image.open(image_path).convert("RGBA")
    x, y = (index % 3) * 512, (index // 3) * 512
    preview.alpha_composite(image, (x, y))
    draw.text((x + 18, y + 18), name, fill=(210, 245, 248, 255))
preview.save(OUTPUT / "BedroomFurniturePreview.png", optimize=True)

print(f"Prepared six bedroom furniture assets in {OUTPUT}")
