from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Buildings" / "YunjianziBanner" / "processed"
DESTINATION = ROOT / "Content" / "Textures" / "Yunjianzi" / "Buildings"


def crop_visible(image: Image.Image) -> Image.Image:
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError("Generated banner asset contains no visible pixels")
    return image.crop(box)


def fit(image: Image.Image, max_width: int, max_height: int) -> Image.Image:
    scale = min(max_width / image.width, max_height / image.height)
    return image.resize((round(image.width * scale), round(image.height * scale)), Image.Resampling.LANCZOS)


def prepare_base() -> Image.Image:
    source = crop_visible(Image.open(SOURCE / "base_alpha.png").convert("RGBA"))
    sprite = fit(source, 180, 190)
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    x = (canvas.width - sprite.width) // 2
    y = 483 - sprite.height
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def prepare_projection() -> Image.Image:
    source = crop_visible(Image.open(SOURCE / "projection_alpha.png").convert("RGBA"))
    sprite = fit(source, 500, 1900)
    canvas = Image.new("RGBA", (512, 2048), (0, 0, 0, 0))
    x = (canvas.width - sprite.width) // 2
    y = (canvas.height - sprite.height) // 2
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def main() -> None:
    DESTINATION.mkdir(parents=True, exist_ok=True)
    prepare_base().save(DESTINATION / "YunjianziBannerBase.png", optimize=True)
    prepare_projection().save(DESTINATION / "YunjianziBannerProjection.png", optimize=True)
    print(DESTINATION)


if __name__ == "__main__":
    main()
