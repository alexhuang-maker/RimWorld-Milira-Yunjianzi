from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


MOD = Path(__file__).resolve().parents[1]
SOURCE = MOD / "ArtSource" / "GeneratedAssets" / "Gravship" / "YJZ_GravshipComponents_Transparent.png"
OUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Buildings" / "Gravship"


def alpha_crop(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise ValueError("Generated gravship component has no visible pixels")
    return image.crop(bbox)


def fit(image: Image.Image, canvas: tuple[int, int], margin: int) -> Image.Image:
    image = alpha_crop(image)
    scale = min((canvas[0] - 2 * margin) / image.width, (canvas[1] - 2 * margin) / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    image = image.resize(size, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", canvas, (0, 0, 0, 0))
    out.alpha_composite(image, ((canvas[0] - size[0]) // 2, (canvas[1] - size[1]) // 2))
    return out


def fit_thruster(image: Image.Image, canvas: int = 512, margin: int = 8) -> Image.Image:
    """Fill the vanilla thruster mesh instead of preserving concept-art whitespace."""
    image = alpha_crop(image)
    target = (canvas - margin * 2, canvas - margin * 2)
    image = image.resize(target, Image.Resampling.LANCZOS)
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    out.alpha_composite(image, (margin, margin))
    return out


def split_engine_layers(engine: Image.Image) -> tuple[Image.Image, Image.Image]:
    """Separate the jade orb from the engine frame for vanilla-style bobbing."""
    runtime = fit(engine, (768, 768), 18)
    scale = 4
    orb_mask_hi = Image.new("L", (768 * scale, 768 * scale), 0)
    draw = ImageDraw.Draw(orb_mask_hi)
    draw.ellipse((293 * scale, 228 * scale, 492 * scale, 440 * scale), fill=255)
    orb_mask = orb_mask_hi.resize((768, 768), Image.Resampling.LANCZOS)

    clear_mask_hi = Image.new("L", (768 * scale, 768 * scale), 0)
    clear_draw = ImageDraw.Draw(clear_mask_hi)
    clear_draw.ellipse((289 * scale, 220 * scale, 503 * scale, 447 * scale), fill=255)
    clear_mask = clear_mask_hi.resize((768, 768), Image.Resampling.LANCZOS)

    orb = Image.new("RGBA", runtime.size, (0, 0, 0, 0))
    orb.paste(runtime, (0, 0), orb_mask)
    frame = runtime.copy()
    frame.paste(Image.new("RGBA", runtime.size, (0, 0, 0, 0)), (0, 0), clear_mask)
    return frame, orb


def save_directional(stem: str, north: Image.Image, canvas: int = 512) -> None:
    north = fit_thruster(north, canvas)
    rotations = {"north": 0, "east": -90, "south": 180, "west": 90}
    for direction, angle in rotations.items():
        image = north if angle == 0 else north.rotate(angle, resample=Image.Resampling.BICUBIC)
        image.save(OUT / f"{stem}_{direction}.png")
    north.resize((256, 256), Image.Resampling.LANCZOS).save(OUT / f"{stem}_MenuIcon.png")


def main() -> None:
    sheet = Image.open(SOURCE).convert("RGBA")
    # Stable x-ranges of the three separated subjects from the generated sheet.
    engine = alpha_crop(sheet.crop((80, 90, 755, 780)))
    small = alpha_crop(sheet.crop((825, 90, 1100, 780)))
    large = alpha_crop(sheet.crop((1150, 90, 1705, 780)))

    OUT.mkdir(parents=True, exist_ok=True)
    engine_runtime, engine_orb = split_engine_layers(engine)
    engine_runtime.save(OUT / "InverseGravEngine.png")
    engine_orb.save(OUT / "InverseGravEngine_Orb.png")
    fit(engine, (256, 256), 8).save(OUT / "InverseGravEngine_MenuIcon.png")
    save_directional("SpiritFlowThruster", small)
    save_directional("CelestialThruster", large)


if __name__ == "__main__":
    main()
