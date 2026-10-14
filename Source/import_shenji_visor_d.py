"""Import the approved wearable Concept-D visor into Milira apparel slots."""

from pathlib import Path

import numpy as np
from PIL import Image


MOD_ROOT = Path(__file__).resolve().parents[1]
GENERATED = MOD_ROOT / "ArtSource" / "GeneratedAssets" / "Shenji"
OUTPUT = MOD_ROOT / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"


def clean_source(name: str) -> Image.Image:
    image = Image.open(GENERATED / name).convert("RGBA")
    alpha = np.asarray(image.getchannel("A")).copy()
    # Image generation may leave an almost invisible dark haze. Runtime Cutout
    # textures must have a genuinely empty exterior.
    alpha[alpha < 32] = 0
    image.putalpha(Image.fromarray(alpha.astype("uint8")))
    return image


def visible(image: Image.Image) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("Generated visor source is empty")
    return image.crop(bounds)


def place_exact(art: Image.Image, box: tuple[int, int, int, int]) -> Image.Image:
    left, top, right, bottom = box
    art = art.resize((right - left, bottom - top), Image.Resampling.LANCZOS)
    result = Image.new("RGBA", (512, 512))
    result.alpha_composite(art, (left, top))
    return result


def save_direction(image: Image.Image, direction: str) -> None:
    image.save(OUTPUT / f"ShenjiVisor_{direction}.png")
    image.save(OUTPUT / f"ShenjiVisor_Female_{direction}.png")


def main() -> None:
    south = visible(clean_source("ShenjiVisor_D_Wearable_south_source.png"))

    east_full = visible(clean_source("ShenjiVisor_D_Wearable_east_source.png"))
    # The generated source includes the portion of the spectacle arm hidden by
    # hair. Milira draws overhead apparel above hair, so import only the exposed
    # lens and hinge housing. The hidden arm remains implied by the design.
    east = east_full.crop((round(east_full.width * 0.46), 0, east_full.width, east_full.height))

    north = visible(clean_source("ShenjiVisor_D_Wearable_north_source.png"))

    # X centers and lower edges match GoggleArmored exactly. Heights stay within
    # its mature envelope while retaining the previous in-game eye-line.
    # Concept D is a wraparound lens rather than the Centurion's short front
    # plate. Keep its proven eye-line and x=256.5 center, but extend both ends
    # to the trusted Shenji hair silhouette so it visibly embraces the head.
    south_runtime = place_exact(south, (169, 246, 345, 304))
    east_runtime = place_exact(east, (247, 250, 314, 304))
    north_runtime = place_exact(north, (182, 245, 331, 274))

    save_direction(south_runtime, "south")
    save_direction(east_runtime, "east")
    save_direction(north_runtime, "north")
    south_runtime.save(OUTPUT / "ShenjiVisor.png")

    # Deliberately no west asset: RimWorld mirrors east, matching Milira's own
    # GoggleArmored convention and preventing direction-specific drift.


if __name__ == "__main__":
    main()
