from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Recreation" / "Guzheng" / "Guzheng_Transparent.png"
OUTPUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Buildings" / "Recreation"
QA_OUTPUT = ROOT / "ArtSource" / "Recreation" / "Guzheng" / "Guzheng_Directional_QA.png"


def crop_subject(image: Image.Image, padding: int = 20) -> Image.Image:
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("The guzheng source contains no visible pixels.")
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    return image.crop((left, top, right, bottom))


def fit_to_canvas(image: Image.Image, canvas_size: tuple[int, int], margin: int) -> Image.Image:
    target_width = canvas_size[0] - margin * 2
    target_height = canvas_size[1] - margin * 2
    scale = min(target_width / image.width, target_height / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", canvas_size, (0, 0, 0, 0))
    x = (canvas.width - resized.width) // 2
    y = (canvas.height - resized.height) // 2
    canvas.alpha_composite(resized, (x, y))
    return canvas


def build_assets() -> dict[str, Image.Image]:
    source = crop_subject(Image.open(SOURCE).convert("RGBA"))
    south = fit_to_canvas(source, (1024, 384), 22)
    north = south.rotate(180, resample=Image.Resampling.BICUBIC, expand=False)
    east = south.rotate(90, resample=Image.Resampling.BICUBIC, expand=True)
    west = south.rotate(-90, resample=Image.Resampling.BICUBIC, expand=True)
    return {"south": south, "north": north, "east": east, "west": west}


def render_qa(images: dict[str, Image.Image]) -> None:
    preview = Image.new("RGB", (1500, 980), (20, 30, 38))
    draw = ImageDraw.Draw(preview)
    font = ImageFont.load_default()
    draw.text((36, 24), "Yunjianzi Guzheng - 3x1 directional and interaction-cell QA", fill=(210, 240, 242), font=font)

    panels = {
        "south": (30, 70, 735, 465),
        "north": (765, 70, 1470, 465),
        "east": (30, 505, 735, 950),
        "west": (765, 505, 1470, 950),
    }
    for direction, rect in panels.items():
        x0, y0, x1, y1 = rect
        draw.rectangle(rect, fill=(28, 43, 54), outline=(100, 196, 202), width=2)
        draw.text((x0 + 12, y0 + 10), direction.upper(), fill=(235, 226, 180), font=font)
        cell = 92
        if direction in ("south", "north"):
            grid_w, grid_h = cell * 3, cell
        else:
            grid_w, grid_h = cell, cell * 3
        gx = (x0 + x1 - grid_w) // 2
        gy = (y0 + y1 - grid_h) // 2
        for ix in range(0, grid_w + 1, cell):
            draw.line((gx + ix, gy, gx + ix, gy + grid_h), fill=(78, 104, 116), width=1)
        for iy in range(0, grid_h + 1, cell):
            draw.line((gx, gy + iy, gx + grid_w, gy + iy), fill=(78, 104, 116), width=1)

        sprite = images[direction].copy()
        sprite.thumbnail((grid_w + 100, grid_h + 100), Image.Resampling.LANCZOS)
        px = (x0 + x1 - sprite.width) // 2
        py = (y0 + y1 - sprite.height) // 2
        preview.paste(sprite.convert("RGB"), (px, py), sprite.getchannel("A"))

        if direction == "south":
            marker = (gx + grid_w // 2, gy + grid_h + 45)
        elif direction == "north":
            marker = (gx + grid_w // 2, gy - 45)
        elif direction == "east":
            marker = (gx + grid_w + 45, gy + grid_h // 2)
        else:
            marker = (gx - 45, gy + grid_h // 2)
        mx, my = marker
        draw.ellipse((mx - 16, my - 16, mx + 16, my + 16), fill=(71, 224, 230), outline=(255, 255, 255), width=2)
        draw.text((mx - 8, my - 5), "P", fill=(8, 31, 38), font=font)

    QA_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    preview.save(QA_OUTPUT)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    images = build_assets()
    for direction, image in images.items():
        image.save(OUTPUT / f"Guzheng_{direction}.png", optimize=True)
    render_qa(images)
    print(f"Wrote guzheng textures to {OUTPUT}")
    print(f"Wrote QA preview to {QA_OUTPUT}")


if __name__ == "__main__":
    main()
