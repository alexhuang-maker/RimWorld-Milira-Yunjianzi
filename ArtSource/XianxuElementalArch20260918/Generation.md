# 纯元素仙絮·头顶拱形

使用内置 imagegen 修改纯元素正面，左右镜像后打包；侧面沿用上一版。原始正面：Elemental_South_Source.png。四方向检查预览：FourDirections_QA.png。

## 提交提示词

Use case: precise-object-edit. Edit this transparent PURE ELEMENTAL lightning game sprite. Change ONLY the top connecting segment: its CENTER MUST RISE UPWARD, making a convex tall rounded arch ABOVE THE WEARER'S HEAD, instead of dipping down in the middle as it does now. Imagine an inverted U or a rainbow arch, NOT a heart, NOT butterfly wings, NOT an M, NOT a bowl. The highest point is exactly at the horizontal center x50%, near y5%; the left and right shoulders descend smoothly from this high center to x22%/78%, y25%. Keep the arch hollow beneath it, leaving the face region fully transparent. Top arch is a slender curved stream of cyan-white electricity with a soft violet halo, same thickness and electrical material as the existing streamers. Maintain STRICT left-right mirror symmetry including the arcing top, both side trails and fine lightning filaments. Keep the lower two S-shaped electric streamers and their curled tips as close to the original as possible, reaching about y95%, retaining similar width and empty central torso area. Keep all elements inside the canvas; the central arch must be higher than the outer shoulders. No fabric, no physical ribbon, no metal, no gold, no jewelry, no opaque surfaces, no character, no letters, no background. True transparent alpha background. Same square canvas, elegant restrained cyan-white lightning cores and violet corona. Important silhouette: upper-middle is a single visibly raised rounded summit. Remove the current concave center completely.

## 分层与粒子

沿用既有前后分层和全部变换参数。中央雷带向上隆起，粒子轨迹从顶部中央向两侧和下方游走。动态光点、电弧和脉冲由 Source/YunjianziXianxuParticles.cs 绘制，暂停不累积、离图不显示，正面两侧使用镜像路径及相同时间相位。未进入游戏验证。

