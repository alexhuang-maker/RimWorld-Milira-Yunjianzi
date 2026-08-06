"""Rebuild Shenji armour as true Milian apparel overlays.

The old generated PNGs redrew an entire humanoid body, including limbs. Milira's
renderer then drew its own body/head over them, producing a sideways duplicate.
These assets preserve the exact canvas, alpha silhouette and anchor of proven
native Milian apparel: Guard, Guardian and Paladin armour.
"""

from pathlib import Path
from PIL import Image


MOD = Path(__file__).resolve().parents[1]
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"
SOURCE_ROOT = REF / "Milian" / "Apparel" / "Middle"
OUTPUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"
TIERS = {
    "Base": "GuardArmor",
    "Intermediate": "GuardianArmor",
    "Final": "PaladinArmor",
}


def recolor(source: Image.Image, tier: str) -> Image.Image:
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
            warm_accent = red > 120 and green > 105 and blue + 16 < green
            if warm_accent:
                color = (35, 222, 218) if tier != "Final" else (104, 247, 239)
            elif luminance < 34:
                color = (17, 24, 28)
            elif luminance < 82:
                color = (7, 45, 53)
            elif luminance < 142:
                color = (72, 126, 129)
            elif luminance < 205:
                color = (177, 211, 207)
            else:
                color = (237, 247, 243)
            dst[x, y] = (*color, alpha)
    return result


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for tier, source_name in TIERS.items():
        built = {}
        for direction in ("south", "east", "north"):
            source_path = SOURCE_ROOT / source_name / f"{source_name}_Female_{direction}.png"
            source = Image.open(source_path).convert("RGBA")
            armour = recolor(source, tier)
            if armour.getchannel("A").getbbox() != source.getchannel("A").getbbox():
                raise RuntimeError(f"{tier}/{direction}: native apparel anchor changed")
            destination = OUTPUT / f"ShenjiArmor_{tier}_Female_{direction}.png"
            armour.save(destination)
            built[direction] = armour
            print(destination)
        built["south"].save(OUTPUT / f"ShenjiArmor_{tier}.png")


if __name__ == "__main__":
    main()
