"""Install an approved side silhouette while preserving approved front/back art."""

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps

from build_cloud_qilin_v10 import keep_largest, normalize


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV11"
SIDE_SOURCE = ART / "CloudQilin_SlimSide_v11_alpha.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn" / "SkyWyvern"


def checker(width: int, height: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (20, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, height, 32):
        for x in range(0, width, 32):
            if (x // 32 + y // 32) % 2 == 0:
                draw.rectangle((x, y, x + 31, y + 31), fill=(31, 47, 58, 255))
    return canvas


def main(side_source: Path = SIDE_SOURCE, art: Path = ART,
         qa_name: str = "CloudQilin_SlimSide_Runtime_QA_v11.png") -> None:
    side = keep_largest(Image.open(side_source).convert("RGBA"))
    # The source faces right, so it is the east sprite. West must be its exact
    # mirror to prevent the two horizontal directions drifting apart again.
    east = normalize(side, True)
    west = ImageOps.mirror(east)

    sprites = {
        "south": Image.open(OUT / "UnifiedGround_south.png").convert("RGBA"),
        "east": east,
        "north": Image.open(OUT / "UnifiedGround_north.png").convert("RGBA"),
        "west": west,
    }
    for direction, sprite in sprites.items():
        sprite.save(OUT / f"UnifiedGround_{direction}.png", optimize=True)

    qa = checker(4608, 512)
    x = 0
    for direction in ("south", "east", "north", "west"):
        qa.alpha_composite(sprites[direction], (x, 0))
        x += sprites[direction].width
    art.mkdir(parents=True, exist_ok=True)
    qa_path = art / qa_name
    qa.save(qa_path, optimize=True)
    print(qa_path)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=SIDE_SOURCE)
    parser.add_argument("--art", type=Path, default=ART)
    parser.add_argument("--qa-name", default="CloudQilin_SlimSide_Runtime_QA_v11.png")
    args = parser.parse_args()
    main(args.source, args.art, args.qa_name)
