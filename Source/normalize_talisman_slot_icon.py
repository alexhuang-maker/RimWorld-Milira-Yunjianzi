from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
target = ROOT / "Content" / "Textures" / "Yunjianzi" / "UI" / "TalismanSlot.png"
reference = ROOT / "Content" / "Textures" / "Yunjianzi" / "UI" / "Abilities" / "SummonZhuxie.png"

generated = Image.open(target).convert("RGBA")
frame = Image.open(reference).convert("RGBA").resize((256, 256), Image.Resampling.LANCZOS)

# Remove the generated thick border, then restore the exact production icon frame.
interior = generated.crop((17, 17, 239, 239)).resize((256, 256), Image.Resampling.LANCZOS)
mask = Image.new("L", (256, 256), 0)
mp = mask.load()
fp = frame.load()
for y in range(256):
    for x in range(256):
        geometry = (x < 18 or x > 237 or y < 18 or y > 237 or
                    (x < 56 and y < 56) or (x > 199 and y < 56) or
                    (x < 56 and y > 199) or (x > 199 and y > 199) or
                    (115 <= x <= 141 and (y <= 29 or y >= 226)) or
                    (115 <= y <= 141 and (x <= 29 or x >= 226)))
        # Select the actual jade/silver frame pixels, not its navy backdrop.
        if geometry and max(fp[x, y][:3]) >= 105:
            mp[x, y] = 255
interior.paste(frame, (0, 0), mask)
interior.save(target)
