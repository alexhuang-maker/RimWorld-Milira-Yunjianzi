"""Build the selected Option-B ground dragon into RimWorld runtime textures."""

from pathlib import Path

import build_ground_dragon_v3 as core
from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "ArtSource" / "GeneratedAssets" / "GroundDragonV4"

core.ART = ART
core.TURNAROUND = ART / "GroundDragon_Turnaround_v4_alpha.png"
core.WALK = {
    "east": ART / "GroundDragon_WalkEast8_v4_alpha.png",
    "south": ART / "GroundDragon_WalkSouth8_v4_alpha.png",
    "north": ART / "GroundDragon_WalkNorth8_v4_alpha.png",
}


def main() -> None:
    core.OUT.mkdir(parents=True, exist_ok=True)
    turnaround = Image.open(core.TURNAROUND).convert("RGBA")
    alpha = turnaround.getchannel("A")
    occupied = [alpha.crop((x, 0, x + 1, alpha.height)).getbbox() is not None for x in range(alpha.width)]
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
    if len(runs) != 4:
        raise RuntimeError(f"Expected four turnaround silhouettes, found {runs}")

    static: dict[str, Image.Image] = {}
    for direction, (left, right) in zip(core.DIRECTIONS, runs):
        sprite = turnaround.crop((max(0, left - 4), 0, min(turnaround.width, right + 4), turnaround.height))
        static[direction] = core.normalize(core.keep_largest(sprite))
    # Runtime west is an exact mirror of east. This prevents pose/scale drift when
    # the pawn turns left while still retaining the generated west view as art reference.
    static["west"] = ImageOps.mirror(static["east"])
    for direction in core.DIRECTIONS:
        static[direction].save(core.OUT / f"UnifiedGround_{direction}.png")

    frames: dict[str, list[Image.Image]] = {}
    for direction in ("south", "east", "north"):
        atlas = Image.open(core.WALK[direction]).convert("RGBA")
        frames[direction] = [core.cell(atlas, index, 8) for index in range(8)]
    frames["west"] = [ImageOps.mirror(frame) for frame in frames["east"]]
    for direction in core.DIRECTIONS:
        for index, frame in enumerate(frames[direction]):
            frame.save(core.OUT / f"UnifiedWalkFrame{index}_{direction}.png")

    qa = core.checker(2048, 1280)
    for column, direction in enumerate(core.DIRECTIONS):
        qa.alpha_composite(static[direction], (column * 512, 0))
    for index, frame in enumerate(frames["east"]):
        qa.alpha_composite(frame.resize((256, 256), Image.Resampling.LANCZOS), (index * 256, 512))
    for index, frame in enumerate(frames["north"]):
        qa.alpha_composite(frame.resize((256, 256), Image.Resampling.LANCZOS), (index * 256, 768))
    for index, frame in enumerate(frames["south"]):
        qa.alpha_composite(frame.resize((256, 256), Image.Resampling.LANCZOS), (index * 256, 1024))
    qa.save(ART / "GroundDragon_Runtime_QA_v4.png")

    gif_frames = [frame.resize((384, 384), Image.Resampling.LANCZOS) for frame in frames["east"]]
    gif_frames[0].save(
        ART / "GroundDragon_Walk_East_v4.gif",
        save_all=True,
        append_images=gif_frames[1:],
        duration=110,
        loop=0,
        disposal=2,
    )


if __name__ == "__main__":
    main()
