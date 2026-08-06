"""Build Shenji's visor on Milira Guard's native goggle geometry.

The Guard does not bake its goggle into hair.  It equips Milian_Goggle as
Milian_OverheadBase apparel, supplies south/east textures at native head
coordinates, lets Graphic_Multi mirror east for west, and uses a transparent
north texture.  This builder preserves those exact silhouettes and anchors;
only the material palette and a clipped cyan sensor line are changed.
"""

from pathlib import Path
from PIL import Image, ImageDraw


MOD = Path(__file__).resolve().parents[1]
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"
SOURCE = REF / "Milian" / "Apparel" / "OverHead" / "Goggle"
OUTPUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"


def jade_recolor(source: Image.Image) -> Image.Image:
    source = source.convert("RGBA")
    result = Image.new("RGBA", source.size, (0, 0, 0, 0))
    src = source.load()
    dst = result.load()
    for y in range(source.height):
        for x in range(source.width):
            red, green, blue, alpha = src[x, y]
            if alpha == 0:
                continue
            luminance = (red * 30 + green * 59 + blue * 11) // 100
            if luminance < 36:
                color = (17, 25, 29)
            elif luminance < 96:
                color = (15, 66, 71)
            elif luminance < 176:
                color = (145, 190, 187)
            else:
                color = (231, 244, 239)
            dst[x, y] = (*color, alpha)
    return result


def clipped_sensor_line(image: Image.Image, direction: str) -> None:
    original_alpha = image.getchannel("A")
    accent = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(accent)
    if direction == "south":
        draw.line((213, 282, 256, 290, 299, 282), fill=(45, 231, 226, 255), width=3, joint="curve")
    elif direction == "east":
        draw.line((263, 282, 305, 280), fill=(45, 231, 226, 255), width=3)
    accent.putalpha(Image.composite(accent.getchannel("A"), Image.new("L", image.size, 0), original_alpha))
    image.alpha_composite(accent)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    built = {}
    for direction in ("south", "east", "north"):
        source = Image.open(SOURCE / f"Goggle_{direction}.png")
        visor = jade_recolor(source)
        clipped_sensor_line(visor, direction)
        source_bbox = source.convert("RGBA").getchannel("A").getbbox()
        visor_bbox = visor.getchannel("A").getbbox()
        if visor_bbox != source_bbox:
            raise RuntimeError(f"{direction} anchor changed: source={source_bbox}, visor={visor_bbox}")
        destination = OUTPUT / f"ShenjiVisor_Female_{direction}.png"
        visor.save(destination)
        built[direction] = visor
        print(destination)
    built["south"].save(OUTPUT / "ShenjiVisor.png")


if __name__ == "__main__":
    main()
