from pathlib import Path

from PIL import Image


MOD_ROOT = Path(__file__).resolve().parents[1]
SOURCE = MOD_ROOT / "ArtSource" / "AbilityIcons" / "Yunjianzi_AbilityIcons_Concept_v1.png"
DESTINATION = MOD_ROOT / "Content" / "Textures" / "Yunjianzi" / "UI" / "Abilities"
DIRECT_SOURCES = {
    "SummonZhuxie.png": MOD_ROOT / "ArtSource" / "AbilityIcons" / "SummonZhuxie_corrected_v2.png",
    "BlinkStrike.png": MOD_ROOT / "ArtSource" / "AbilityIcons" / "BlinkStrike_corrected_v2.png",
}

# Each crop keeps one complete ornamental frame from the approved three-panel
# concept sheet. The source is 2048x768; all three crops are square before the
# final downsample, so RimWorld does not stretch the characters or borders.
CROPS = {
    "ImmortalArray.png": (1366, 49, 2026, 709),
}


def main() -> None:
    source = Image.open(SOURCE).convert("RGBA")
    DESTINATION.mkdir(parents=True, exist_ok=True)
    for filename, source_path in DIRECT_SOURCES.items():
        icon = Image.open(source_path).convert("RGBA").resize((512, 512), Image.Resampling.LANCZOS)
        icon.save(DESTINATION / filename, optimize=True)
    for filename, bounds in CROPS.items():
        icon = source.crop(bounds).resize((512, 512), Image.Resampling.LANCZOS)
        icon.save(DESTINATION / filename, optimize=True)


if __name__ == "__main__":
    main()
