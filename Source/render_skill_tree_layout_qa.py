"""Render a deterministic QA contact sheet for the in-game skill-tree layout."""

from __future__ import annotations

import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Source" / "YunjianziSkillTree.cs"
OUTPUT = ROOT / "ArtSource" / "GeneratedAssets" / "Yunjianzi_SkillTree_Layout_QA.png"
TITLES = ("御剑诀", "仙阵诀", "神机诀")
SUBTITLES = (
    "附灵、护体、剑影、瞬斩、天剑雨、护主与破甲",
    "回元、雷域、阵纹、阵遁、随身阵、灵盾与界壁",
    "主火力、折射、机动、护盾、浮游炮、接口、归元与控制",
)
LANES = (
    ("附灵", "护体·同契", "剑影", "瞬斩", "天剑雨", "护主", "破甲"),
    ("回元", "雷域", "阵纹", "阵遁", "随身阵", "灵盾", "界壁"),
    ("主火力", "折射", "机动", "护盾", "浮游炮", "接口", "败阵归元", "控制"),
)
ROOT_IDS = ("SummonZhuxie", "ArrayCore", "ShenjiSummon")
REALMS = ("练气", "筑基", "紫府", "金丹", "化神")


def font(size: int, bold: bool = False):
    path = Path(r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc")
    return ImageFont.truetype(str(path), size)


def parse_nodes():
    pattern = re.compile(
        r'^\s*N\((\w+),\s*"([^"]+)",\s*"[^"]*",\s*(\d+),\s*(\d+),\s*(\d+)(?:,\s*([^\)]*))?\),?'
    )
    nodes = []
    for line in SOURCE.read_text(encoding="utf-8").splitlines():
        match = pattern.match(line)
        if not match:
            continue
        symbol, label, realm, branch, lane, prerequisites = match.groups()
        nodes.append(
            {
                "id": symbol,
                "label": label,
                "realm": int(realm),
                "branch": int(branch),
                "lane": int(lane),
                "prerequisites": [item.strip() for item in (prerequisites or "").split(",") if item.strip()],
            }
        )
    return nodes


def line(draw, points, fill, width=3):
    draw.line(points, fill=fill, width=width, joint="curve")


def render_page(branch: int, nodes):
    width, height = 1400, 650
    image = Image.new("RGB", (width, height), (8, 24, 30))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((10, 10, width - 10, height - 10), 12, fill=(12, 39, 46), outline=(91, 183, 191), width=2)
    draw.text((width // 2, 20), TITLES[branch], font=font(28, True), fill=(232, 248, 248), anchor="ma")
    draw.text((width // 2, 53), SUBTITLES[branch], font=font(15), fill=(155, 210, 214), anchor="ma")

    content_x, content_w = 100, 1275
    lanes = LANES[branch]
    step = content_w / len(lanes)
    for lane, label in enumerate(lanes):
        draw.text((content_x + step * (lane + .5), 82), label, font=font(15, True), fill=(137, 213, 218), anchor="mm")

    row_start, row_step = 180, 88
    for realm, label in enumerate(REALMS, 1):
        y = row_start + (realm - 1) * row_step
        fill = (14, 48, 55) if realm % 2 else (16, 55, 62)
        draw.rectangle((22, y - 8, width - 22, y + 60), fill=fill)
        draw.text((57, y + 26), label, font=font(15, True), fill=(121, 220, 222), anchor="mm")

    branch_nodes = [node for node in nodes if node["branch"] == branch]
    by_id = {node["id"]: node for node in branch_nodes}
    rects = {}
    root = ROOT_IDS[branch]
    rects[root] = (width / 2 - 90, 104, width / 2 + 90, 154)
    node_width = min(148, step - 14)
    for node in branch_nodes:
        if node["id"] == root:
            continue
        cx = content_x + step * (node["lane"] + .5)
        y = row_start + (node["realm"] - 1) * row_step
        rects[node["id"]] = (cx - node_width / 2, y, cx + node_width / 2, y + 52)

    for node in branch_nodes:
        child = rects[node["id"]]
        for prerequisite in node["prerequisites"]:
            if prerequisite not in rects:
                continue
            parent = rects[prerequisite]
            px, py = (parent[0] + parent[2]) / 2, parent[3]
            cx, cy = (child[0] + child[2]) / 2, child[1]
            if prerequisite == root:
                bus = parent[3] + 14
                line(draw, ((px, py), (px, bus), (cx, bus), (cx, cy)), (76, 139, 145), 3)
            elif abs((parent[1] + parent[3]) - (child[1] + child[3])) < 2:
                line(draw, ((parent[2], (parent[1] + parent[3]) / 2), (child[0], (child[1] + child[3]) / 2)), (76, 139, 145), 3)
            else:
                line(draw, ((px, py), (cx, cy)), (76, 139, 145), 3)

    for node in branch_nodes:
        rect = rects[node["id"]]
        learned = node["id"] == "SummonZhuxie"
        fill = (41, 151, 158) if learned else (46, 62, 65)
        draw.rounded_rectangle(rect, 5, fill=fill, outline=(110, 216, 220) if learned else (95, 108, 110), width=2)
        draw.text(((rect[0] + rect[2]) / 2, (rect[1] + rect[3]) / 2), node["label"], font=font(14, True), fill=(244, 249, 249), anchor="mm")
    return image


def main():
    nodes = parse_nodes()
    assert len(nodes) == 42, len(nodes)
    pages = [render_page(branch, nodes) for branch in range(3)]
    sheet = Image.new("RGB", (1400, 1990), (5, 16, 21))
    draw = ImageDraw.Draw(sheet)
    draw.text((28, 15), "云间子修行面板 · 依赖导向布局本地验收", font=font(28, True), fill=(225, 247, 248))
    for index, page in enumerate(pages):
        sheet.paste(page, (0, 55 + index * 645))
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT, optimize=True)
    print(OUTPUT)


if __name__ == "__main__":
    main()
