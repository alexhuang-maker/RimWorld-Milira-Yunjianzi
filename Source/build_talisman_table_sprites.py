from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "Yangfu" / "TalismanTable_3D_Transparent.png"
OUT = ROOT / "Content" / "Textures" / "Yunjianzi" / "Building"
OUT.mkdir(parents=True, exist_ok=True)

south = Image.open(SOURCE).convert("RGBA").resize((512, 768), Image.Resampling.LANCZOS)
south.save(OUT / "TalismanTable.png")
south.save(OUT / "TalismanTable_south.png")
south.rotate(180, expand=True, resample=Image.Resampling.BICUBIC).save(OUT / "TalismanTable_north.png")
south.rotate(90, expand=True, resample=Image.Resampling.BICUBIC).save(OUT / "TalismanTable_east.png")
south.rotate(270, expand=True, resample=Image.Resampling.BICUBIC).save(OUT / "TalismanTable_west.png")
