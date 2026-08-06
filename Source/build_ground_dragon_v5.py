"""Build the tucked-leg, static four-direction ground dragon textures."""

from pathlib import Path

from PIL import Image, ImageOps

import build_ground_dragon_v3 as core


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV5"
SOURCE = ART / "GroundDragon_TuckedTurnaround_v5_alpha.png"


def silhouette_runs(alpha: Image.Image) -> list[tuple[int, int]]:
    occupied = [
        alpha.crop((x, 0, x + 1, alpha.height)).getbbox() is not None
        for x in range(alpha.width)
    ]
    runs: list[tuple[int, int]] = []
    start = None
    last = None
    for x, used in enumerate(occupied):
        if used and start is None:
            start = x
        if used:
            last = x
        if start is not None and last is not None and x - last > 12:
            runs.append((start, last + 1))
            start = None
            last = None
    if start is not None and last is not None:
        runs.append((start, last + 1))
    return runs


def main() -> None:
    core.OUT.mkdir(parents=True, exist_ok=True)
    source = Image.open(SOURCE).convert("RGBA")
    runs = silhouette_runs(source.getchannel("A"))
    if len(runs) != 4:
        raise RuntimeError(f"Expected four silhouettes, found {runs}")

    sprites: dict[str, Image.Image] = {}
    for direction, (left, right) in zip(core.DIRECTIONS, runs):
        crop = source.crop((max(0, left - 4), 0, min(source.width, right + 4), source.height))
        sprites[direction] = core.normalize(core.keep_largest(crop))
    sprites["west"] = ImageOps.mirror(sprites["east"])

    for direction in core.DIRECTIONS:
        sprites[direction].save(core.OUT / f"UnifiedGround_{direction}.png")

    qa = core.checker(2048, 512)
    for column, direction in enumerate(core.DIRECTIONS):
        qa.alpha_composite(sprites[direction], (column * 512, 0))
    qa.save(ART / "GroundDragon_Runtime_QA_v5.png")


if __name__ == "__main__":
    main()
