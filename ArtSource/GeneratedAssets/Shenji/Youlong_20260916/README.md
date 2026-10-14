# 神机武器 B「游龙」定稿 — 2026-09-16

用户选择流线方案 B。使用内置 image_gen 分别生成贯日、九霄；保留生成的透明通道。

运行时贴图：
- `Content/Textures/Yunjianzi/Weapons/ShenjiCannon_Gauss.png`
- `Content/Textures/Yunjianzi/Weapons/ShenjiCannon_Legendary.png`

本目录保存两张原始生成母图（`*_master.png`），不随运行时部署。
选定参考图：`C:/Users/alex/.codex/generated_images/01a07637-d34d-7161-a571-a7cf4f99387e/exec-e3092c6f-08a1-404c-936d-82985f6e7635.png`，中间 B 行。

## 整理及检查

仓库 `tools/Pack-ShenjiYoulongTextures.ps1` 只做等比缩放和透明画布排版，不重绘。
保持原有 512×512 画布、横向占幅，并通过枪口青色像素确定原贴图的发射轴，误差小于 1 像素。
新贯日可见范围：x=90，y=187，宽332，高115；九霄：x=85，y=196，宽341，高110。
检查透明背景、边缘不裁切、显示占幅、枪口轴与缩小后的轮廓。
对照预览及旧图备份：仓库 `output/shenji-youlong-20260916/`。
预览是贴图合成检查，不是 RimWorld 实机截图。未修改武器数值、显示尺寸或射线机制。

## 原始生成提示词

两次调用共用以下前缀，再分别附加对应武器段落；参考图相同。

```text
Use case: precise-object-edit / game sprite extraction. Input is the approved concept board. Produce ONE final isolated weapon sprite, not a concept sheet. Follow the specified B 游龙 row weapon faithfully: preserve its actual silhouette, flowing ivory/jade shells, thin antique gold outlines, small integrated ivory grip, almond cyan-white core, and level right-facing emission channel. Clean illustrated 2D RimWorld weapon texture, flat side orthographic view, crisp dark contour, broad readable shading. Do NOT redesign, add ornaments, add literal dragon heads, replace with other row, add extra weapons or perspective. Remove all background, text, labels and other weapons. Genuine transparent alpha PNG background, not gray/checkerboard pixels. No contact shadow, glow outside silhouette, external firing beam, floor, border or watermark. Cyan-white light has almost white center with narrow pale cyan edge. Ivory/gold body opaque. Square 1024x1024 canvas. Weapon faces RIGHT horizontally. Keep weapon art centered, with its leftmost pixel near x180, rightmost near x846, topmost near y366, and lowermost grip near y652, leaving generous completely transparent padding around it. Do not crop tips or grip. The central firing channel lies horizontally near y500. The requested weapon only.
```

贯日：

```text
Select exactly the LEFT weapon in the MIDDLE B 游龙 row, beneath header 贯日. This is GUANRI: single compact flowing dragon-spine jade upper ridge, small gold-lined almond core at rear, straight narrow beam channel, smooth tapered pointed right muzzle. Preserve the compact single-stage shape from that reference.
```

九霄：

```text
Select exactly the RIGHT weapon in the MIDDLE B 游龙 row, beneath header 九霄. This is JIUXIAO: longer developed two-layer flowing dragon-spine jade ridge, large almond cyan-white core at rear and a SECOND small diamond/almond bright core near middle where the curved lower shell joins the straight luminous channel, elegant split jade muzzle at right. Preserve the reference's second core and full long enclosed flowing lower contour; not the simpler left weapon.
```
