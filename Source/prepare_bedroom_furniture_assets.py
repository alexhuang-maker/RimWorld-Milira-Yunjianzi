from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\alex\.codex\generated_images\019fc1fd-4fb5-7700-8fe1-53b8f7bf3ae2")
ART_SOURCE = ROOT / "ArtSource/Bedroom"
QA_OUTPUT = ART_SOURCE / "QA"
OUTPUT = ROOT / "Content/Textures/Yunjianzi/Buildings/Bedroom"
OUTPUT.mkdir(parents=True, exist_ok=True)
QA_OUTPUT.mkdir(parents=True, exist_ok=True)


SOURCES = {
    "SereneBed": ART_SOURCE / "SereneBed_Wide_Magenta.png",
    "Nightstand": "exec-b86c49ff-81f4-4caf-9bca-c07133e85809.png",
    "Chair": "exec-60a8f315-f02b-4f2e-9402-0cb1046e1844.png",
    "SmallTable": "exec-38593687-9ac0-4132-a8b4-27b5da06daa1.png",
    "Wardrobe": "exec-8993d8b5-db74-42f9-9593-a2f5f1df5a1e.png",
    "WallBookcase": "exec-e8224250-2163-4b8c-9d32-b579f65b5e4e.png",
}


def remove_magenta(source: Path, crop_by_alpha: bool = False) -> Image.Image:
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
    # RGB values survive in fully transparent pixels after spill removal.  An
    # RGBA getbbox() therefore measures the old magenta canvas and can place a
    # visibly off-centre object on the final game texture.  Crop by meaningful
    # alpha instead, so the rendered object—not its invisible RGB—is centred.
    alpha_mask = image.getchannel("A").point(lambda value: 255 if value > 64 else 0)
    box = alpha_mask.getbbox() if crop_by_alpha else image.getbbox()
    return image.crop(box) if box else image


def on_canvas(image: Image.Image, size: tuple[int, int], occupancy: float = .90,
              crop_by_alpha: bool = False) -> Image.Image:
    canvas = Image.new("RGBA", size)
    image = image.copy()
    if crop_by_alpha:
        alpha_box = image.getchannel("A").point(lambda value: 255 if value > 64 else 0).getbbox()
        if alpha_box:
            image = image.crop(alpha_box)
    image.thumbnail((int(size[0] * occupancy), int(size[1] * occupancy)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    return canvas


def save_single(name: str, image: Image.Image, occupancy: float = .90) -> None:
    on_canvas(image, (512, 512), occupancy).save(OUTPUT / f"{name}.png", optimize=True)


def save_multi(name: str, south: Image.Image, occupancy: float = .90,
               rotate_long_axis: bool = False, crop_by_alpha: bool = False) -> None:
    # A generated pseudo-3D front must not be rotated by 180 degrees. Doing so
    # turns doors, chair backs and cornices physically upside down. One-cell
    # furniture stays upright in every rotation; long-axis furniture only gets
    # sideways East/West variants for its footprint.
    directions = {"south": south, "north": south, "east": south, "west": south}
    if rotate_long_axis:
        directions["east"] = south.rotate(90, expand=True, resample=Image.Resampling.BICUBIC)
        directions["west"] = south.rotate(270, expand=True, resample=Image.Resampling.BICUBIC)
    for direction, image in directions.items():
        on_canvas(image, (512, 512), occupancy, crop_by_alpha).save(OUTPUT / f"{name}_{direction}.png", optimize=True)


clean = {
    name: remove_magenta(source if isinstance(source, Path) else GENERATED / source,
                         crop_by_alpha=name == "WallBookcase")
    for name, source in SOURCES.items()
}
save_multi("SereneBed", clean["SereneBed"], .96, rotate_long_axis=True)
save_single("Nightstand", clean["Nightstand"], .88)
save_multi("Chair", clean["Chair"], .84)
save_single("SmallTable", clean["SmallTable"], .88)
save_multi("Wardrobe", clean["Wardrobe"], .88)
save_multi("WallBookcase", clean["WallBookcase"], .88, crop_by_alpha=True)

# Building_Bookcase draws separate top-altitude bookend layers.  Transparent
# one-pixel sprites keep the vanilla renderer active without adding foreign art.
transparent = Image.new("RGBA", (8, 8))
transparent.save(OUTPUT / "WallBookcase_Bookend_east.png", optimize=True)
transparent.save(OUTPUT / "WallBookcase_Bookend_north.png", optimize=True)

# Four-direction contact sheet used for local scale/orientation QA.
preview = Image.new("RGBA", (len(SOURCES) * 384, 4 * 384), (15, 26, 35, 255))
draw = ImageDraw.Draw(preview)
for column, name in enumerate(SOURCES):
    for row, direction in enumerate(("south", "east", "north", "west")):
        directional = OUTPUT / f"{name}_{direction}.png"
        image_path = directional if directional.exists() else OUTPUT / f"{name}.png"
        image = Image.open(image_path).convert("RGBA")
        image.thumbnail((340, 340), Image.Resampling.LANCZOS)
        x, y = column * 384, row * 384
        preview.alpha_composite(image, (x + (384 - image.width) // 2, y + (384 - image.height) // 2))
        draw.text((x + 12, y + 12), f"{name} / {direction}", fill=(210, 245, 248, 255))
preview.save(QA_OUTPUT / "BedroomFurnitureRotationQA.png", optimize=True)

# Approximate the North-facing native Placeworker_AttachedToWall placement at
# 256 px per cell. The visible shelf must remain inside the wall cell rather
# than hanging down into the room's floor cell.
wall_qa = Image.new("RGBA", (768, 768), (66, 71, 74, 255))
wall_draw = ImageDraw.Draw(wall_qa)
for gx in range(0, 769, 256):
    wall_draw.line((gx, 0, gx, 768), fill=(110, 126, 132, 255), width=2)
for gy in range(0, 769, 256):
    wall_draw.line((0, gy, 768, gy), fill=(110, 126, 132, 255), width=2)
wall_draw.rectangle((256, 0, 511, 255), fill=(37, 46, 50, 255), outline=(8, 10, 12, 255), width=8)
wall_draw.rectangle((268, 12, 499, 243), fill=(91, 101, 105, 255), outline=(154, 166, 168, 255), width=5)
wall_draw.line((384, 0, 384, 256), fill=(91, 226, 235, 255), width=2)
bookcase = Image.open(OUTPUT / "WallBookcase_north.png").convert("RGBA")
bookcase.thumbnail((276, 276), Image.Resampling.LANCZOS)
base_x = 384 - bookcase.width // 2
base_y = 384 - bookcase.height // 2
offset_y = round(-1.05 * 256)
wall_qa.alpha_composite(bookcase, (base_x, base_y + offset_y))
wall_draw.text((18, 724), "Native wall attachment / offset 1.05 / no floor overlap", fill=(210, 245, 248, 255))
wall_qa.save(QA_OUTPUT / "WallBookcasePlacementQA.png", optimize=True)

print(f"Prepared six bedroom furniture assets in {OUTPUT}")
