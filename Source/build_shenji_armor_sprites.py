from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter


MOD = Path(__file__).resolve().parents[1]
REF = MOD.parent / "参考Mod" / "必需" / "Milira Race [3256974620]" / "Content" / "Textures"
OUT = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel" / "Shenji"
QA = MOD / "ArtSource" / "Shenji" / "ShenjiArmor_InGameLayers_QA.png"
ROBE = MOD / "Content" / "Textures" / "Yunjianzi" / "Apparel"
ORTHO = MOD / "ArtSource" / "GeneratedAssets" / "Shenji" / "Shenji_Evolution_Orthographic_v1.png"

INK = (18, 27, 30, 255)
JADE = (238, 247, 243, 255)
JADE_SHADE = (188, 219, 217, 255)
TEAL = (8, 57, 65, 255)
DEEP = (4, 34, 43, 255)
CYAN = (37, 226, 224, 255)
GOLD = (188, 146, 62, 255)


def poly(draw, points, fill=JADE, width=6):
    draw.polygon(points, fill=fill, outline=INK, width=width)


def line(draw, points, fill=GOLD, width=4):
    draw.line(points, fill=fill, width=width, joint="curve")


def gem(layer, xy, radius, stage):
    glow = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((xy[0]-radius*2, xy[1]-radius*2, xy[0]+radius*2, xy[1]+radius*2), fill=(29, 239, 232, 80))
    glow = glow.filter(ImageFilter.GaussianBlur(max(2, radius // 2)))
    layer.alpha_composite(glow)
    d = ImageDraw.Draw(layer)
    d.ellipse((xy[0]-radius-3, xy[1]-radius-3, xy[0]+radius+3, xy[1]+radius+3), fill=GOLD, outline=INK, width=4)
    d.ellipse((xy[0]-radius+2, xy[1]-radius+2, xy[0]+radius-2, xy[1]+radius-2), fill=CYAN)
    if stage >= 3:
        d.ellipse((xy[0]-radius//2, xy[1]-radius//2, xy[0]+radius//2, xy[1]+radius//2), fill=(225, 255, 251, 255))


def front_overlay(stage):
    layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    # 独立米莉安机械躯干，不再以云间子道袍作为底图。
    poly(d, [(216, 225), (296, 225), (309, 318), (285, 367), (227, 367), (203, 318)], DEEP)
    poly(d, [(205, 247), (226, 259), (218, 342), (191, 365), (177, 341), (185, 271)], TEAL)
    poly(d, [(307, 247), (286, 259), (294, 342), (321, 365), (335, 341), (327, 271)], TEAL)
    poly(d, [(222, 350), (252, 353), (247, 444), (218, 461), (203, 441)], JADE)
    poly(d, [(290, 350), (260, 353), (265, 444), (294, 461), (309, 441)], JADE)
    # 共同的机械仙甲胸骨与肩甲，保持米莉安身体的颈部/手臂开口。
    poly(d, [(205, 219), (232, 205), (256, 220), (280, 205), (307, 219), (294, 254), (256, 269), (218, 254)])
    poly(d, [(183, 215), (214, 210), (224, 234), (199, 249), (172, 240)])
    poly(d, [(329, 215), (298, 210), (288, 234), (313, 249), (340, 240)])
    d.polygon([(229, 224), (256, 211), (283, 224), (273, 287), (256, 306), (239, 287)], fill=TEAL, outline=INK, width=6)
    line(d, [(238, 240), (256, 257), (274, 240)], CYAN, 4)
    line(d, [(256, 258), (256, 296)], CYAN, 4)
    gem(layer, (256, 236), 10 + stage * 2, stage)
    # 腰部白玉护片。
    poly(d, [(208, 302), (239, 285), (252, 307), (226, 355), (192, 377)])
    poly(d, [(304, 302), (273, 285), (260, 307), (286, 355), (320, 377)])
    line(d, [(209, 319), (230, 309), (241, 318)], GOLD, 4)
    line(d, [(303, 319), (282, 309), (271, 318)], GOLD, 4)
    if stage >= 2:
        # 紫府型：增加分层肩羽、前臂护甲和双侧灵力节点。
        poly(d, [(166, 212), (201, 193), (225, 205), (205, 229), (176, 239), (153, 228)])
        poly(d, [(346, 212), (311, 193), (287, 205), (307, 229), (336, 239), (359, 228)])
        poly(d, [(166, 255), (195, 245), (211, 273), (193, 322), (160, 333), (149, 305)])
        poly(d, [(346, 255), (317, 245), (301, 273), (319, 322), (352, 333), (363, 305)])
        gem(layer, (182, 271), 8, stage)
        gem(layer, (330, 271), 8, stage)
        line(d, [(171, 296), (191, 286), (195, 311)], CYAN, 5)
        line(d, [(341, 296), (321, 286), (317, 311)], CYAN, 5)
    if stage >= 3:
        # 化神型：八角主核心与向下展开的白玉剑羽轮廓。
        gem(layer, (256, 272), 22, stage)
        for side in (-1, 1):
            sx = 256 + side * 55
            pts = [(sx, 326), (256 + side * 88, 344), (256 + side * 101, 411),
                   (256 + side * 70, 395), (256 + side * 48, 357)]
            poly(d, pts)
            line(d, [(sx, 341), (256 + side * 79, 370), (256 + side * 88, 397)], CYAN, 5)
        line(d, [(242, 271), (256, 249), (270, 271), (256, 294), (242, 271)], GOLD, 5)
    return layer


def side_overlay(stage):
    layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    poly(d, [(223, 222), (281, 222), (291, 329), (265, 372), (223, 352), (207, 288)], DEEP)
    poly(d, [(217, 257), (239, 265), (231, 348), (205, 370), (191, 341), (198, 281)], TEAL)
    poly(d, [(243, 349), (274, 351), (272, 446), (245, 461), (229, 437)], JADE)
    poly(d, [(221, 208), (251, 199), (279, 218), (281, 262), (247, 278), (218, 253)])
    d.polygon([(235, 220), (259, 211), (274, 228), (267, 281), (245, 294), (231, 268)], fill=TEAL, outline=INK, width=6)
    gem(layer, (252, 232), 10 + stage, stage)
    poly(d, [(218, 299), (248, 281), (267, 302), (250, 361), (216, 385), (203, 353)])
    if stage >= 2:
        poly(d, [(202, 214), (237, 191), (263, 207), (241, 235), (209, 247), (190, 234)])
        poly(d, [(207, 250), (235, 242), (249, 269), (234, 327), (200, 339), (190, 305)])
        gem(layer, (216, 274), 8, stage)
        line(d, [(208, 296), (229, 284), (231, 316)], CYAN, 5)
    if stage >= 3:
        gem(layer, (252, 272), 20, stage)
        poly(d, [(244, 327), (283, 348), (288, 411), (260, 393), (235, 355)])
        line(d, [(254, 344), (276, 366), (280, 397)], CYAN, 5)
    return layer


def back_overlay(stage):
    layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    poly(d, [(216, 225), (296, 225), (309, 322), (282, 370), (230, 370), (203, 322)], DEEP)
    poly(d, [(205, 248), (226, 259), (218, 342), (191, 365), (177, 341), (185, 271)], TEAL)
    poly(d, [(307, 248), (286, 259), (294, 342), (321, 365), (335, 341), (327, 271)], TEAL)
    poly(d, [(222, 352), (252, 355), (247, 444), (218, 461), (203, 441)], JADE)
    poly(d, [(290, 352), (260, 355), (265, 444), (294, 461), (309, 441)], JADE)
    poly(d, [(205, 211), (234, 198), (256, 216), (278, 198), (307, 211), (295, 259), (256, 276), (217, 259)])
    d.polygon([(230, 224), (256, 211), (282, 224), (273, 284), (256, 301), (239, 284)], fill=TEAL, outline=INK, width=6)
    gem(layer, (256, 238), 10 + stage * 2, stage)
    poly(d, [(208, 302), (242, 284), (254, 309), (228, 366), (191, 386)])
    poly(d, [(304, 302), (270, 284), (258, 309), (284, 366), (321, 386)])
    if stage >= 2:
        poly(d, [(167, 214), (205, 190), (231, 205), (208, 234), (178, 246), (151, 230)])
        poly(d, [(345, 214), (307, 190), (281, 205), (304, 234), (334, 246), (361, 230)])
        gem(layer, (190, 227), 8, stage)
        gem(layer, (322, 227), 8, stage)
    if stage >= 3:
        gem(layer, (256, 276), 21, stage)
        poly(d, [(221, 328), (187, 350), (181, 414), (211, 397), (240, 355)])
        poly(d, [(291, 328), (325, 350), (331, 414), (301, 397), (272, 355)])
        line(d, [(211, 350), (194, 375), (190, 402)], CYAN, 5)
        line(d, [(301, 350), (318, 375), (322, 402)], CYAN, 5)
    return layer


def largest_alpha_component(image):
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    seen = bytearray(width * height)
    largest = []
    for y in range(height):
        for x in range(width):
            index = y * width + x
            if seen[index] or pixels[x, y] < 24:
                continue
            stack = [(x, y)]
            seen[index] = 1
            component = []
            while stack:
                cx, cy = stack.pop()
                component.append((cx, cy))
                for nx, ny in ((cx-1, cy), (cx+1, cy), (cx, cy-1), (cx, cy+1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    ni = ny * width + nx
                    if not seen[ni] and pixels[nx, ny] >= 24:
                        seen[ni] = 1
                        stack.append((nx, ny))
            if len(component) > len(largest):
                largest = component
    mask = Image.new("L", image.size, 0)
    mp = mask.load()
    for x, y in largest:
        mp[x, y] = pixels[x, y]
    result = image.copy()
    result.putalpha(mask)
    return result


def build_sprite(stage, direction):
    # 采用用户选定的旧版“短发＋战术目镜”正交稿，而非云间子道袍式新概念。
    source = Image.open(ORTHO).convert("RGBA")
    x_ranges = {"south": (0, 342), "east": (342, 685), "north": (685, 1028)}
    y_ranges = {1: (0, 370), 2: (370, 755), 3: (755, 1148)}
    x0, x1 = x_ranges[direction]
    y0, y1 = y_ranges[stage]
    cell = source.crop((x0, y0, x1, y1))
    character = largest_alpha_component(cell)
    bbox = character.getbbox()
    if bbox is None:
        raise RuntimeError(f"No Shenji character found for stage={stage}, direction={direction}")
    character = character.crop(bbox)
    target_height = {1: 330, 2: 350, 3: 372}[stage]
    target_width = max(1, round(character.width * target_height / character.height))
    character = character.resize((target_width, target_height), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    canvas.alpha_composite(character, ((512-target_width)//2, 478-target_height))
    return canvas


def build_visor(direction):
    layer = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    if direction == "south":
        poly(d, [(198, 176), (314, 176), (304, 220), (256, 232), (208, 220)], JADE, 7)
        d.polygon([(211, 205), (301, 205), (292, 217), (256, 225), (220, 217)], fill=TEAL, outline=INK, width=4)
        line(d, [(225, 211), (287, 211)], CYAN, 5)
    elif direction == "east":
        poly(d, [(216, 178), (279, 183), (277, 216), (232, 229), (213, 213)], JADE, 7)
        line(d, [(225, 210), (269, 204)], CYAN, 5)
    else:
        # 背面只露出目镜两侧的固定节点。
        gem(layer, (203, 205), 6, 1)
        gem(layer, (309, 205), 6, 1)
    return layer


def checker(size):
    img = Image.new("RGBA", size, (20, 31, 39, 255))
    d = ImageDraw.Draw(img)
    s = 24
    for y in range(0, size[1], s):
        for x in range(0, size[0], s):
            if (x // s + y // s) % 2 == 0:
                d.rectangle((x, y, x+s-1, y+s-1), fill=(31, 47, 58, 255))
    return img


def composite_preview(stage, direction, armor):
    body = Image.open(REF / "Milian" / "Pawn" / "Body" / f"Naked_Female_{direction}.png").convert("RGBA")
    canvas = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    # 实际游戏中该固有装甲跳过 Head/Hair，自身包含固定短发与战术目镜；
    # 米莉安裸体仅作为被完全覆盖的底层保留碰撞和受伤结构。
    for layer in (body, armor):
        canvas = Image.alpha_composite(canvas, layer)
    return canvas


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    QA.parent.mkdir(parents=True, exist_ok=True)
    directions = ("south", "east", "north")
    tiers = ((1, "Base"), (2, "Intermediate"), (3, "Final"))
    previews = {}
    for stage, name in tiers:
        for direction in directions:
            sprite = build_sprite(stage, direction)
            sprite.save(OUT / f"ShenjiArmor_{name}_Female_{direction}.png")
            previews[(stage, direction)] = composite_preview(stage, direction, sprite)
        # 物品图标使用南向图，Graphic_Multi 会读取带方向的穿戴图。
        build_sprite(stage, "south").save(OUT / f"ShenjiArmor_{name}.png")

    tile = 300
    sheet = checker((tile * 3, tile * 3 + 70))
    sd = ImageDraw.Draw(sheet)
    labels = {1: "练气 / 筑基", 2: "紫府 / 金丹", 3: "化神"}
    for row, (stage, _) in enumerate(tiers):
        for col, direction in enumerate(directions):
            pawn = previews[(stage, direction)]
            crop = pawn.crop((120, 135, 392, 510)).resize((218, 300), Image.Resampling.LANCZOS)
            sheet.alpha_composite(crop, (col * tile + 41, row * tile + 5))
            sd.text((col * tile + 10, row * tile + 272), f"{labels[stage]}  {direction}", fill=(217, 246, 244, 255), stroke_width=2, stroke_fill=(7, 16, 22, 255))
    sd.text((18, tile * 3 + 18), "实际游戏层级：米莉安身体（被覆盖）→ 固有神机全身甲；Head/Hair 跳过，短发与目镜不会重复。", fill=(135, 224, 225, 255))
    sheet.save(QA)
    print(QA)


if __name__ == "__main__":
    main()
