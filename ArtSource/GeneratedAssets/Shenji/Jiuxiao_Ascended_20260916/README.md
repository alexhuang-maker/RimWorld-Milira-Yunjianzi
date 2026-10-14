# 九霄「游龙」终阶强化版

2026-09-16：用户确认强化预览并要求实装，对照其他武器的表现形式检查。

运行时文件：`Content/Textures/Yunjianzi/Weapons/ShenjiCannon_Legendary.png`。
本目录 `ShenjiCannon_Legendary_master.png` 保留内置 image_gen 生成的透明原图。
生成原图：`exec-578cd18d-d06c-4c02-be20-7d61bf39b4f7.png`。
不重新生成、不改画面，只将已批准的图等比缩放、平移至现有透明画布。

## 与同系列及原版渲染方式对照

- 青芒、连星、贯日、九霄均为朝右的 512×512 透明 PNG，使用 `Graphic_Single`、`Cutout`。
- 九霄原有 `drawSize=(1.72,1.18)`、文件路径、Def、装备设置保持不变。
- RimSage 核对 `PawnRenderUtility.DrawEquipmentAiming`：瞄准角减90度，并在200至340度区间翻转武器；缩放由 `Graphic.drawSize` 决定。
- 父 Def `Gun_AssaultRifle` 使用单图武器，并继承地面随机旋转35度；无需四方向贴图。
- 检查帝国的两处 `DrawEquipmentAiming` 补丁：特殊绘制隐藏要求 `ThingDef_Custom`，持握偏移要求 `WeaponDrawOffsetExtension`。九霄未使用这些配置。
- 输出边界 x=84，y=186，宽342，高117；枪口青色像素平均轴从248.03变为247.54，误差约0.49像素。
- 检查整圈画布边缘完全透明、Cutout可见像素非空、无同名运行时贴图覆盖。
- 青芒、连星、贯日 PNG 与神机 XML 的 SHA256 均与本次修改前一致。

验证工具：仓库 `tools/Check-JiuxiaoTexture.ps1`。
整理工具：仓库 `tools/Pack-ShenjiYoulongTextures.ps1`，本次只传入 JiuxiaoSource。
报告、修改前备份和八方向预览：仓库 `output/jiuxiao-youlong-final-20260916/`。
预览采用 XML 长宽比例与原版转向规则，不是游戏截图；尚未进行实机角色遮挡、后坐力和运行中其他模组补丁的验证。

## 内置 image_gen 原始提示词

参考图1：上一版九霄母图；参考图2：保留不改的贯日母图。

```text
Use case: precise-object-edit / stylized game weapon concept. Image 1 is the edit target, current JIUXIAO 九霄 游龙 beam artifact weapon. Image 2 is its lower-tier GUANRI counterpart, a comparison reference only, leave that design unchanged and do NOT include it in output. The art director says current Jiuxiao looks too similar in power to Guanri. Redesign ONLY Jiuxiao to look unmistakably like a much more powerful ultimate-tier xianxia divine weapon, while preserving the approved graceful STREAMLINED flowing-dragon design language and ivory jade, restrained antique gold, cyan-white energy palette. Deliver one isolated high-quality revised concept preview of the new Jiuxiao. Genuine transparent background, PNG alpha, no text or labels. Weapon entirely visible, horizontal orthographic side profile aiming RIGHT, centered with clear empty margins. Crisp painted 2D RimWorld game sprite illustration, thick clean dark edge, simple readable major masses, broad restrained shading.
Make the transformation substantial, a decisive silhouette upgrade, NOT a subtle recolor or extra tiny details. The revised artifact has a more substantial central body, about 30 percent deeper than current, while still long and sleek. A large elongated faceted cyan-WHITE dragon-heart crystal in a gold-edged almond frame replaces the small rear eye, protected by three smooth sweeping overlapping ivory shell ridges, with one noble long upswept gold-edged crown-like crest merging into the body, no literal dragon head. Elegant S-curved upper and lower contour. The front half evolves from a thin tube into a formidable elongated split-jaw focusing lance: a broad top and bottom tapering ivory blade-shell, plus ONE slim floating outer ivory guide blade closely parallel above the barrel with a narrow clearly visible gap, all sweep forward and converge around a bright white-cyan diamond-shaped focusing crystal near the muzzle. This crystal remains INSIDE the jaws, not an external projectile. A dominant straight cyan-white internal energy spine connects the rear core to the focusing crystal. Use narrow quiet gold inset channels leading forward to communicate concentrated power. The muzzle opening and forward jaws are clearly more substantial than the original, not a tiny pointed rifle muzzle. Closely connected compact structure, no loose orbiting debris. Small integrated ivory grip under rear third, not a black tactical handle. Strong striking shape even as a small sprite. Show power through clear silhouette, core size and focusing architecture, NOT excessive glow. White core, pale cyan rim, subtle teal recesses, no intense blue bloom. No conventional gun stock, no external circular rings, no jagged feather fans, no blocky armor chunks, no dangling ribbons, no huge random spikes, no swords hilt/crossguard, no real dragon eyes or head, no projected beam, no muzzle flash, no explosive effects, no realistic rendering. Keep flowing and sophisticated. Do not produce multiple weapons or a comparison board.
```
