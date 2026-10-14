# 麒麟雷爪：最终选用版本

使用内置 image_gen 工具生成和编辑，2026-09-08。

## 运行素材

- `Content/Textures/Yunjianzi/Effects/QilinClaws/ThunderClawA.png`：用户选中的第一张三道斜抓痕，保留生成原图。
- `Content/Textures/Yunjianzi/Effects/QilinClaws/ThunderClawB.png`：以用户选中的第二张横扫爪痕为参考移除第四道，最终为三道抓痕。
- 四种播放效果为 A、B、A 左右镜像、B 左右镜像。镜像通过原版 `FleckCreationData.exactScale.x < 0` / `Graphic_Fleck.DrawFleck` 实现，复用原图，避免重新绘制镜像造成差异。
- 每次随机排除上一种，记录在麒麟组件的 `yjz_wyvernLastClawVariant` 存档字段中。旧存档默认 -1；视觉随机独立于战斗随机序列。
- 特效持续 0.34 游戏秒，短暂显现后淡出。伤害、范围、冷却和控制效果沿用现有雷爪逻辑。
- 大小按原来麒麟到目标的电弧长度校准（约 1.35～2.8 格，最终阶段额外 6%）；斜爪按对角线长度补偿缩小。取消播放中的膨胀，避免大于原效果。两张素材均验证为 RGBA，空白区 alpha 为 0、发光边缘有渐变 alpha，未修改原透明通道。
- 原先生成的四爪横扫、交叉爪痕和五爪重击位于 `Unused/`，不参与运行或部署。两次带棋盘格的中间结果未采用。

## 最终提示词 A

Use case: stylized-concept. Asset type: standalone transparent PNG combat VFX sprite for a top-down RimWorld fantasy mod, square 1024x1024. Primary request: a lightning-charged qilin beast CLAW SCRATCH attack effect. Variant A: three separated long tapered talon scratches sweeping diagonally from upper left to lower right; gently hooked parallel arcs, broad middle and needle sharp tips. Clear three-claw silhouette. Style: crisp stylized 2D game effects, legible at small screen size, white-hot thin cores, lavender inner light, saturated violet electric edges, a few thin jagged branching lightning filaments that hug and connect the claw marks. The CLAW SCRATCH SHAPES must dominate; keep electric branches sparse. Center the entire isolated effect within the central 75% of the canvas with generous transparent margins. True transparent background with alpha; no black rectangle, no white background, no checkerboard painted into the image. Smooth alpha around glow, restrained halo. No animal, paw, hand, weapon, person, scene, ground, gore, wound, debris, text, labels, border, frames or contact sheet. Produce one finished reusable effect sprite only.

## 最终提示词 B

Create a transparent-background game VFX sprite: exactly THREE separate curved claw scratches sweeping from left to right, staggered lengths, top longest, bottom shortest, white-hot lightning cores with violet-purple luminous rims and fine branching electrical filaments between the three scratches. Use the attached image as the shape and style reference: keep ONLY its top three claw scratches, omit its fourth bottom scratch. The transparent canvas is square with empty margins. Crisp stylized 2D painted fantasy combat effect. Three curved lightning claw marks only, isolated on transparent background. Preserve the reference's orientation and purple-white color. No extra fourth mark, no labels, no scene.

参考图 B：`exec-50deceb3-5b0d-42d4-a30f-cf774cb8898e.png`；最终透明三爪输出：`exec-2d072f2f-d1a8-4523-905e-9dbe79c18666.png`。生成 alpha 原样保留。
