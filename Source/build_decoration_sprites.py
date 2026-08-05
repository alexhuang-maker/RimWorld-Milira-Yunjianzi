from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Decoration"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Buildings" / "Decoration"
OUT.mkdir(parents=True, exist_ok=True)


def fit_to_canvas(source: Path, size: tuple[int, int], padding: float = 0.055) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    alpha_box = image.getchannel("A").getbbox()
    if alpha_box is None:
        raise ValueError(f"No visible pixels in {source}")
    image = image.crop(alpha_box)
    usable_width = int(size[0] * (1 - 2 * padding))
    usable_height = int(size[1] * (1 - 2 * padding))
    scale = min(usable_width / image.width, usable_height / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(
        resized,
        ((size[0] - resized.width) // 2, (size[1] - resized.height) // 2),
    )
    return canvas


for name in ("EverlightLamp", "CloudCenser", "FloatingJadeDisplay"):
    sprite = fit_to_canvas(SOURCE / f"{name}_Transparent.png", (512, 512))
    sprite.save(OUT / f"{name}.png")

screen = fit_to_canvas(SOURCE / "CloudScreen_Transparent.png", (1024, 512), 0.04)
screen.save(OUT / "CloudScreen.png")
screen.save(OUT / "CloudScreen_south.png")
screen.rotate(180, expand=False, resample=Image.Resampling.BICUBIC).save(
    OUT / "CloudScreen_north.png"
)
screen.rotate(90, expand=True, resample=Image.Resampling.BICUBIC).save(
    OUT / "CloudScreen_east.png"
)
screen.rotate(270, expand=True, resample=Image.Resampling.BICUBIC).save(
    OUT / "CloudScreen_west.png"
)
