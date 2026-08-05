from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter
import colorsys

ROOT = Path(__file__).resolve().parents[1]
GENERATED = Path(r"C:\Users\alex\.codex\generated_images\019fc1fd-4fb5-7700-8fe1-53b8f7bf3ae2")
ABILITY_DIR = ROOT / "Content/Textures/Yunjianzi/UI/Abilities"
EFFECT_DIR = ROOT / "Content/Textures/Yunjianzi/Effects/Yangfu"
ABILITY_DIR.mkdir(parents=True, exist_ok=True)
EFFECT_DIR.mkdir(parents=True, exist_ok=True)

for source_name, target_name in (
    ("exec-6db9ba8a-e1e0-41a9-afb8-5236f48263f4.png", "FireTornado.png"),
    ("exec-f5c59c31-fecb-4f24-80d8-a5509ac00e59.png", "ThunderWind.png"),
    ("exec-38c5c1e4-2fd3-44ef-89a5-ad2f2f718603.png", "BeanSoldiers.png"),
):
    image = Image.open(GENERATED / source_name).convert("RGB")
    image.resize((512, 512), Image.Resampling.LANCZOS).save(ABILITY_DIR / target_name, optimize=True)

def remove_magenta(source_name: str, target_name: str, size: int) -> None:
    image = Image.open(GENERATED / source_name).convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, _ = pixels[x, y]
            distance = ((r - 255) ** 2 + g ** 2 + (b - 255) ** 2) ** 0.5
            alpha = 0 if distance < 40 else (255 if distance > 145 else int((distance - 40) / 105 * 255))
            if alpha < 255:
                # Remove magenta spill from antialiased edge pixels.
                r = max(0, r - (255 - alpha))
                b = max(0, b - (255 - alpha))
            pixels[x, y] = (r, g, b, alpha)
    box = image.getbbox()
    image = image.crop(box) if box else image
    canvas = Image.new("RGBA", (size, size))
    image.thumbnail((int(size * .9), int(size * .9)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(image, ((size - image.width) // 2, (size - image.height) // 2))
    canvas.save(EFFECT_DIR / target_name, optimize=True)

remove_magenta("exec-c4f65308-57f8-4a0b-8011-44a166949102.png", "BeanSoldier.png", 256)
remove_magenta("exec-d8a3a247-9b12-422b-9c80-d028b10ada65.png", "FireTornado.png", 512)

base = Image.open(ROOT / "Content/Textures/Yunjianzi/Talisman/Tiandao.png").convert("RGBA")
for name, tint in (
    ("TalismanRed.png", (232, 48, 38)),
    ("TalismanPurple.png", (147, 62, 226)),
    ("TalismanGreen.png", (45, 202, 100)),
):
    out = base.copy()
    data = []
    for r, g, b, a in out.getdata():
        if a == 0:
            data.append((0, 0, 0, 0))
            continue
        luminance = (r + g + b) / 765.0
        mix = .22 + .78 * luminance
        data.append((int(tint[0] * mix), int(tint[1] * mix), int(tint[2] * mix), a))
    out.putdata(data)
    out.save(EFFECT_DIR / name, optimize=True)

# A restrained purple wind-field decal with transparent center and edge.
field = Image.new("RGBA", (512, 512))
draw = ImageDraw.Draw(field)
center = 256
for radius, alpha, width in ((220, 80, 8), (190, 54, 5), (140, 38, 4)):
    draw.ellipse((center-radius, center-radius, center+radius, center+radius), outline=(155, 95, 255, alpha), width=width)
for i in range(6):
    radius = 65 + i * 25
    start = 25 + i * 53
    draw.arc((center-radius, center-radius, center+radius, center+radius), start, start+145,
             fill=(195, 150, 255, 85-i*7), width=5)
field = field.filter(ImageFilter.GaussianBlur(.7))
field.save(EFFECT_DIR / "ThunderWindField.png", optimize=True)

print("Prepared Yangfu combat icons and effect sprites.")
