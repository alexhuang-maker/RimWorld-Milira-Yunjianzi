# 九霄仙絮 A/C 切换

2026-11-22。用户选择 A 游霆天绶和 C 流光云络，两者可在游戏里切换；隐显统一使用 A 双肩灵扣。

## 使用

佩戴仙絮后，选择角色。使用「仙絮：流光云络／游霆天绶」切换展开外观；使用「仙絮常显」决定平日是否展开。
默认 C、常显关闭：平日 A 灵扣，征召 C。两个选择通过 ThingComp.PostExposeData 保存在仙絮物品上；不改变技能效果。

## 资源

使用内置 image_gen，参考既有三垣秘仪斗 Spirit.png 和用户选中的 A/C 概念图。
运行贴图：Content/Textures/Yunjianzi/Apparel/JiuxiaoXianxu/Styles/{A,C,Hidden}.png。
三张均为 1280×1280 透明 PNG。生成结果直接复制，没有覆盖旧版本贴图。

最终生成提示词结构：

Produce a final game sprite asset, NOT a concept sheet. Output one square 1024x1024 transparent RGBA sprite, no text, border, ground, character, background or panel. Extract/redraw the reference artifact faithfully. Front view mirrored left/right accessory with enormous EMPTY central opening for pawn body. Keep topmost artwork at y=220 px; shoulder clasp centers at x=290 and734,y=280; deployed ends reach y=880; all art only in x=100..350 or674..924, center x=365..659 entirely transparent at all heights. Top 180px entirely transparent to leave head ring free. Thin graceful 2D rendered game sprite, readable polished silver-white edge and gold accents, lavender spectral mesh with authentic variable transparency, light luminous edges but NO opaque white haze. Do not include reference text or character.

- A: Full deployed A: TWO narrow S-shaped lavender spectral ribbons with silver gold thin clasps and sparse fine violet electric threads. Match main large artifact in reference.
- C: Full deployed C: TWO weightless open geometric silver gold cloud-lattice shoulder frames, violet translucent veil trails on either side. Match main large artifact in reference.
- Hidden: HIDDEN A form: ONLY two tiny elegant silver-gold shoulder clasps with extremely short faint lavender wisps. NO long ribbons or mantle. Use top clasps of reference at exact same location. Almost entire canvas empty. Only tiny clasps within x250..330 and694..774,y240..320; other pixels transparent.

## 验证与限制

- 正式 1.7 编译通过，0 警告、0 错误。
- ArtifactSkillChecks 共 165 项通过：包含全部 8 种外观状态组合和从当前 XML 装备层构造原版 Def 后，调用实际游戏程序集 ApparelUtility.CanWearTogether 的 6 个有序配对。
- 贴图按 4 像素步长采样：顶部 16% 和中央 x=42%..58% 区域最大 alpha 为 1/255，没有 alpha>8 的可见图形。
- 仙絮改用左右半片独立绘制；侧向远侧降低透明度，前后使用不同绘制高度。前后视图保留对称外观，侧视图宽度压缩为 60%，并后移 0.10 格。属于同一贴图的程序化侧视表现，不是独立手绘侧视图。
- 旧仙絮整体高层披帛绘制不再从装备组件调用。头顶与身体中央留空，减轻与三垣圆轨及剑影争抢轮廓的问题。
- 本机 RimWorld.exe 启动后退出，无可用游戏窗口。尚未完成游戏内四朝向、移动、战斗及倒地情况下的目视叠穿检查。不能将上述验证解释为已经证明完全没有遮挡、穿模或其他模组影响。
