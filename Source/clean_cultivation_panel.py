"""Remove the five legacy realm placeholders baked into the cultivation UI.

The old panel contained five bright diamond placeholders.  The runtime used
dark rectangles to hide them before drawing the real realm emblems, which left
visible boxes and overlapped the lower frame at larger realm sizes.  Rebuild a
clean background by interpolating the untouched pixels on both sides of each
placeholder, then keep the clean master as the reproducible source asset.
"""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource/GeneratedAssets/CultivationUI_master_v1_transparent.png"
CLEAN_MASTER = ROOT / "ArtSource/GeneratedAssets/CultivationUI_master_v2_clean.png"
GAME_PANEL = ROOT / "Content/Textures/Yunjianzi/UI/CultivationPanel.png"


def remove_placeholder(array: np.ndarray, center_x: int) -> None:
    x0, x1 = center_x - 16, center_x + 16
    y0, y1 = 184, 217
    for y in range(y0, y1 + 1):
        left = array[y, x0 - 1].astype(np.float32)
        right = array[y, x1 + 1].astype(np.float32)
        width = x1 - x0 + 1
        for offset, x in enumerate(range(x0, x1 + 1), start=1):
            blend = offset / (width + 1)
            array[y, x] = np.rint(left * (1.0 - blend) + right * blend).astype(np.uint8)


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    bounds = source.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError("Cultivation panel source contains no visible pixels")
    panel = source.crop(bounds).resize((750, 240), Image.Resampling.LANCZOS)
    pixels = np.asarray(panel).copy()
    for center in (408, 459, 510, 562, 613):
        remove_placeholder(pixels, center)
    cleaned = Image.fromarray(pixels, "RGBA")
    cleaned.save(CLEAN_MASTER, optimize=True)
    cleaned.save(GAME_PANEL, optimize=True)
    print(CLEAN_MASTER)
    print(GAME_PANEL)


if __name__ == "__main__":
    main()
