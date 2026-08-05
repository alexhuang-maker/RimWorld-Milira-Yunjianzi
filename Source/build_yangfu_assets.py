from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageOps
import math

ROOT = Path(__file__).resolve().parents[1]
TALISMAN = ROOT / "Content" / "Textures" / "Yunjianzi" / "Talisman"
BUILDING = ROOT / "Content" / "Textures" / "Yunjianzi" / "Building"
QA = ROOT / "ArtSource" / "Yangfu"
for folder in (TALISMAN, BUILDING, QA):
    folder.mkdir(parents=True, exist_ok=True)

ITEMS = [
    ("Zhuoyu", "啄玉", 2), ("Shenjie", "神截", 2), ("Shenxing", "神行", 2),
    ("Jiqing", "寄情", 2), ("Baicao", "百草", 2), ("Xinjian", "心剑", 2),
    ("Beidou", "北斗", 3), ("Guihai", "归海", 3), ("Shenlian", "神炼", 3),
    ("Huijian", "惠剑", 3), ("Bumei", "不昧", 4), ("Jiexin", "劫心", 4),
    ("Liuyang", "六阳", 4), ("Tiandao", "天道", 5), ("Huanri", "换日", 5),
]
TIER = {2: (77, 194, 174, 255), 3: (112, 146, 235, 255), 4: (232, 178, 64, 255), 5: (218, 114, 230, 255)}


def glow_line(draw, points, color, width=7):
    draw.line(points, fill=(8, 38, 43, 230), width=width + 5, joint="curve")
    draw.line(points, fill=color, width=width, joint="curve")


def icon(stem, realm, idx):
    im = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    shadow = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle((57, 30, 199, 228), radius=18, fill=(0, 0, 0, 145))
    shadow = shadow.filter(ImageFilter.GaussianBlur(10))
    im.alpha_composite(shadow)
    d = ImageDraw.Draw(im)
    paper = (238, 250, 239, 255) if realm < 5 else (244, 236, 255, 255)
    edge = TIER[realm]
    d.rounded_rectangle((49, 20, 191, 218), radius=18, fill=(28, 57, 60, 255), outline=(213, 190, 111, 255), width=5)
    d.rounded_rectangle((58, 29, 182, 209), radius=13, fill=paper, outline=edge, width=5)
    d.polygon([(58, 29), (83, 29), (58, 54)], fill=(207, 231, 221, 255))
    d.polygon([(182, 209), (157, 209), (182, 184)], fill=(207, 231, 221, 255))
    d.ellipse((104, 43, 136, 75), fill=(15, 70, 73, 255), outline=(213, 190, 111, 255), width=4)
    d.ellipse((113, 52, 127, 66), fill=edge)
    # One consistent talisman language; every index changes the central seal.
    cx, cy = 120, 129
    color = (31, 142, 145, 255) if realm < 5 else (161, 69, 192, 255)
    sides = 3 + idx % 6
    radius = 43
    pts = [(cx + math.cos(-math.pi / 2 + 2 * math.pi * k / sides) * radius,
            cy + math.sin(-math.pi / 2 + 2 * math.pi * k / sides) * radius) for k in range(sides)]
    glow_line(d, pts + [pts[0]], color, 5)
    if idx % 3 == 0:
        d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), outline=color, width=6)
        glow_line(d, [(cx, cy - 35), (cx, cy + 35)], color, 5)
    elif idx % 3 == 1:
        glow_line(d, [(cx - 31, cy + 27), (cx, cy - 31), (cx + 31, cy + 27)], color, 6)
        d.ellipse((cx - 9, cy - 9, cx + 9, cy + 9), fill=edge)
    else:
        glow_line(d, [(cx - 32, cy), (cx - 10, cy - 24), (cx + 9, cy + 24), (cx + 32, cy)], color, 6)
        d.arc((cx - 28, cy - 28, cx + 28, cy + 28), 205, 520, fill=edge, width=5)
    for yy in (184, 196):
        d.line((86, yy, 154, yy), fill=(203, 174, 92, 230), width=3)
    im.save(TALISMAN / f"{stem}.png")
    return im


icons = [icon(stem, realm, i) for i, (stem, _, realm) in enumerate(ITEMS)]


def table_south():
    im = Image.new("RGBA", (512, 768), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # 2x3 footprint, white-jade frame and dark teal work surface.
    d.rounded_rectangle((55, 48, 457, 716), radius=38, fill=(19, 47, 54, 255), outline=(19, 25, 29, 255), width=14)
    d.rounded_rectangle((70, 62, 442, 702), radius=30, fill=(218, 235, 224, 255), outline=(201, 168, 84, 255), width=8)
    d.rounded_rectangle((91, 89, 421, 674), radius=22, fill=(18, 78, 82, 255), outline=(112, 225, 218, 255), width=6)
    for y in range(132, 652, 78):
        d.line((110, y, 402, y), fill=(46, 123, 123, 150), width=3)
    # Paper, seal, inkstone, brushes and material dishes.
    d.rounded_rectangle((145, 180, 354, 524), radius=12, fill=(240, 247, 231, 255), outline=(213, 180, 95, 255), width=6)
    d.ellipse((204, 246, 296, 338), fill=(30, 82, 83, 255), outline=(216, 184, 94, 255), width=6)
    d.ellipse((221, 263, 279, 321), outline=(91, 225, 217, 255), width=7)
    d.line((250, 270, 250, 314), fill=(91, 225, 217, 255), width=7)
    d.line((228, 292, 272, 292), fill=(91, 225, 217, 255), width=7)
    d.rounded_rectangle((104, 546, 197, 632), radius=16, fill=(12, 34, 38, 255), outline=(208, 177, 93, 255), width=6)
    d.ellipse((124, 568, 177, 615), fill=(3, 13, 15, 255), outline=(84, 190, 185, 255), width=4)
    for x, c in ((337, (103, 232, 219, 255)), (382, (226, 189, 80, 255))):
        d.ellipse((x - 23, 559, x + 23, 605), fill=(229, 241, 226, 255), outline=(207, 174, 88, 255), width=5)
        d.ellipse((x - 11, 571, x + 11, 593), fill=c)
    for i in range(3):
        x = 112 + i * 21
        d.line((x, 125, x + 170, 592), fill=(219, 190, 111, 255), width=8)
        d.line((x + 170, 592, x + 178, 615), fill=(53, 164, 159, 255), width=5)
    return im


south = table_south()
south.save(BUILDING / "TalismanTable_south.png")
ImageOps.flip(south).save(BUILDING / "TalismanTable_north.png")
east = south.rotate(90, expand=False)
east.save(BUILDING / "TalismanTable_east.png")
ImageOps.mirror(east).save(BUILDING / "TalismanTable_west.png")
south.save(BUILDING / "TalismanTable.png")

sheet = Image.new("RGB", (1280, 940), (13, 26, 31))
sd = ImageDraw.Draw(sheet)
for i, ((_, label, realm), image) in enumerate(zip(ITEMS, icons)):
    x = 26 + (i % 5) * 180
    y = 24 + (i // 5) * 278
    sheet.paste(image.resize((160, 160)), (x, y), image.resize((160, 160)))
    sd.text((x + 42, y + 168), f"R{realm} {label}", fill=(220, 235, 231))
preview = south.resize((256, 384))
sheet.paste(preview, (948, 84), preview)
sd.text((985, 480), "2x3 Talisman Table", fill=(220, 235, 231))
sheet.save(QA / "YangfuAssets_QA.png")
