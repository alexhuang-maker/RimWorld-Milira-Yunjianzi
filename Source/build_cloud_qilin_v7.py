"""Build the legless mirrored side-profile cloud qilin runtime textures."""

from pathlib import Path

from PIL import Image, ImageOps

import build_cloud_qilin_v6 as v6
import build_ground_dragon_v3 as core


ROOT = Path(__file__).resolve().parents[1]
V6_SOURCE = (
    ROOT
    / "ArtSource"
    / "GeneratedAssets"
    / "GroundDragonV6"
    / "CloudQilin_AbstractTurnaround_v6_alpha.png"
)
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV7"
SIDE_SOURCE = ART / "CloudQilin_LeglessSide_v7_alpha.png"


def accepted_front_and_back() -> dict[str, Image.Image]:
    """Rebuild the already-approved south/north sprites byte-identically."""
    source = Image.open(V6_SOURCE).convert("RGBA")
    runs = v6.silhouette_runs(source.getchannel("A"))
    if len(runs) != 4:
        raise RuntimeError(f"Expected four V6 silhouettes, found {runs}")

    sprites: dict[str, Image.Image] = {}
    for direction, (left, right) in zip(core.DIRECTIONS, runs):
        crop = source.crop(
            (max(0, left - 4), 0, min(source.width, right + 4), source.height)
        )
        sprites[direction] = core.normalize(core.keep_largest(crop))
    return {"south": sprites["south"], "north": sprites["north"]}


def main() -> None:
    core.OUT.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)

    accepted = accepted_front_and_back()
    # The sole authored profile faces west (left). East is always its exact
    # pixel mirror, preventing independent-art direction or proportion drift.
    west = core.normalize(
        core.keep_largest(Image.open(SIDE_SOURCE).convert("RGBA"))
    )
    east = ImageOps.mirror(west)

    sprites = {
        "south": accepted["south"],
        "east": east,
        "north": accepted["north"],
        "west": west,
    }
    for direction in core.DIRECTIONS:
        sprites[direction].save(core.OUT / f"UnifiedGround_{direction}.png")

    qa = core.checker(2048, 512)
    for column, direction in enumerate(core.DIRECTIONS):
        qa.alpha_composite(sprites[direction], (column * 512, 0))
    qa.save(ART / "CloudQilin_Legless_Runtime_QA_v7.png")
    print(ART / "CloudQilin_Legless_Runtime_QA_v7.png")


if __name__ == "__main__":
    main()
