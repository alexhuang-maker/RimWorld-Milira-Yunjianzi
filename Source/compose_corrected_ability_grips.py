from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / "Content" / "Textures" / "Yunjianzi"
SOURCE = ROOT / "ArtSource" / "AbilityIcons"
PREVIEW = SOURCE / "GripCorrectionPreview"


def alpha_crop(image: Image.Image) -> Image.Image:
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError("Weapon texture has no visible pixels")
    return image.crop(box)


def rotated_weapon(angle: float, target_length: int) -> Image.Image:
    weapon = Image.open(TEXTURES / "Weapons" / "Zhuxie.png").convert("RGBA")
    weapon = alpha_crop(weapon).rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)
    weapon = alpha_crop(weapon)
    scale = target_length / max(weapon.width, weapon.height)
    return weapon.resize(
        (round(weapon.width * scale), round(weapon.height * scale)),
        Image.Resampling.LANCZOS,
    )


def add_glow(base: Image.Image, sprite: Image.Image, position: tuple[int, int], radius: int = 9) -> None:
    glow = Image.new("RGBA", base.size)
    alpha = Image.new("L", base.size)
    alpha.paste(sprite.getchannel("A"), position)
    alpha = alpha.filter(ImageFilter.GaussianBlur(radius))
    cyan = Image.new("RGBA", base.size, (88, 232, 255, 0))
    cyan.putalpha(alpha.point(lambda value: min(115, value)))
    glow.alpha_composite(cyan)
    base.alpha_composite(glow)
    base.alpha_composite(sprite, position)


def conceal_summon_sword(base: Image.Image) -> Image.Image:
    # Rebuild the narrow background strip occupied by the old non-graspable sword.
    # The replacement sword and its glow cover the centre; feathering avoids hard seams.
    x0, y0, x1, y1 = 124, 34, 233, 407
    # Clone a clean strip from farther left. It has the same navy radial background
    # and portal glow, unlike the right edge which contains the pawn's face and arm.
    patch = base.crop((12, y0, 12 + (x1 - x0), y1)).filter(ImageFilter.GaussianBlur(1.2))
    mask = Image.new("L", patch.size, 255)
    feather = Image.new("L", patch.size, 0)
    feather_draw = ImageDraw.Draw(feather)
    feather_draw.rectangle((12, 0, patch.width - 13, patch.height), fill=255)
    feather = feather.filter(ImageFilter.GaussianBlur(8))
    base.paste(patch, (x0, y0), feather)
    return base


def make_summon() -> Image.Image:
    base = Image.open(TEXTURES / "UI" / "Abilities" / "SummonZhuxie.png").convert("RGBA")
    # Preserve the approved point-down summoning composition. The original hilt
    # used ornament all the way from pommel to guard, so redraw its centre as a
    # practical wrapped grip while retaining the jade socket and blade.
    scale = 4
    grip = Image.new("RGBA", (64 * scale, 80 * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(grip)
    # Local coordinates are composited at (148, 38); final grip is x174..184, y58..96.
    draw.rounded_rectangle(
        (25 * scale, 20 * scale, 36 * scale, 59 * scale),
        radius=3 * scale,
        fill=(21, 57, 68, 255),
        outline=(205, 184, 112, 255),
        width=2 * scale,
    )
    draw.line((28 * scale, 22 * scale, 35 * scale, 28 * scale), fill=(224, 248, 244, 235), width=1 * scale)
    for y in (31, 40, 49):
        draw.line((26 * scale, y * scale, 35 * scale, (y + 6) * scale), fill=(101, 220, 222, 235), width=2 * scale)
    # Small pommel cap; the existing upper diamond remains visible above it.
    draw.polygon(
        [(30 * scale, 15 * scale), (38 * scale, 21 * scale), (30 * scale, 27 * scale), (22 * scale, 21 * scale)],
        fill=(229, 247, 239, 255),
        outline=(187, 159, 87, 255),
    )
    grip = grip.resize((64, 80), Image.Resampling.LANCZOS)
    base.alpha_composite(grip, (148, 38))
    return base


def make_blink() -> Image.Image:
    base = Image.open(TEXTURES / "UI" / "Abilities" / "BlinkStrike.png").convert("RGBA")
    sword = rotated_weapon(-65, 252)
    # The hilt sits over the leading hand; the blade follows the existing slash axis.
    position = (191, 272)
    add_glow(base, sword, position, radius=6)
    return base


def main() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    make_summon().save(PREVIEW / "SummonZhuxie_grip_preview.png")
    make_blink().save(PREVIEW / "BlinkStrike_grip_preview.png")
    print(PREVIEW)


if __name__ == "__main__":
    main()
