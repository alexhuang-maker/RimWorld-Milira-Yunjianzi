from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "GeneratedAssets"
TEXTURES = ROOT / "Content" / "Textures" / "Yunjianzi"


def crop_pad_resize(source_name: str, destination: Path, size: int, rotate_ccw: bool = False, margin: float = 0.055) -> None:
    image = Image.open(SOURCE / source_name).convert("RGBA")
    if rotate_ccw:
        image = image.rotate(90, expand=True)

    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= 3 else 0).getbbox()
    if bounds is None:
        raise RuntimeError(f"No visible pixels in {source_name}")

    image = image.crop(bounds)
    usable = max(1, round(size * (1.0 - margin * 2.0)))
    scale = min(usable / image.width, usable / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )

    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((size - resized.width) // 2, (size - resized.height) // 2))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=True)


def crop_resize_exact(source_name: str, destination: Path, width: int, height: int) -> None:
    image = Image.open(SOURCE / source_name).convert("RGBA")
    bounds = image.getchannel("A").point(lambda value: 255 if value >= 3 else 0).getbbox()
    if bounds is None:
        raise RuntimeError(f"No visible pixels in {source_name}")
    image = image.crop(bounds).resize((width, height), Image.Resampling.LANCZOS)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, optimize=True)


crop_pad_resize(
    "Zhuxie_master_v2_transparent.png",
    TEXTURES / "Weapons" / "Zhuxie.png",
    512,
    margin=0.035,
)
crop_pad_resize(
    "SwordShadow_master_v3_transparent.png",
    TEXTURES / "Weapons" / "SwordShadow.png",
    256,
    rotate_ccw=True,
    margin=0.08,
)
crop_pad_resize(
    "ArrayCore_master_v1_transparent.png",
    TEXTURES / "Array" / "ArrayCore.png",
    256,
    margin=0.06,
)
crop_pad_resize(
    "ArrayField_master_v1_transparent.png",
    TEXTURES / "Array" / "ArrayField.png",
    512,
    margin=0.02,
)
crop_resize_exact(
    "CultivationUI_master_v1_transparent.png",
    TEXTURES / "UI" / "CultivationPanel.png",
    750,
    240,
)
