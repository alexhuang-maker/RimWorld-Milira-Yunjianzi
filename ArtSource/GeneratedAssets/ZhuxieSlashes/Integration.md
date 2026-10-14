# 诛邪剑痕：用户选定版本

2026-09-08：用户选定流光粒子版 A（斜劈）与 B（横切），要求各自左右镜像、透明背景、瞬间亮起后消失。

运行素材：

- `Content/Textures/Yunjianzi/Effects/ZhuxieSlashes/SlashA.png`
- `Content/Textures/Yunjianzi/Effects/ZhuxieSlashes/SlashB.png`

两张 PNG 从仓库 `output/imagegen/ZhuxieImmortalCuts/` 原样复制；SHA256 一致，RGBA 的透明背景与渐变 alpha 已检查。原图由内置 image_gen 制作，完整生成提示词在该输出目录的 `Designs.md`。

四个播放变体为 A、B、镜像 A、镜像 B。镜像复用原图，使用原版 `FleckCreationData.exactScale` 的负 X 分量，经 `Graphic_Fleck.DrawFleck` 选择镜像网格。每次随机排除上一次，视觉随机不消耗战斗随机序列；上一变体保存在 `yjz_lastZhuxieSlashVariant` 中，旧存档默认 -1。

诛邪原有普通近战出招入口调用新播放方法；原有境界门槛、攻击判定、伤害及冷却不变。选定图已包含碎光粒子，不再叠加旧弧光、剑形残影或额外交叉弧光。未替换共享给缩地御剑、剑雨的旧 FleckDef。

瞬间亮起（fadeInTime=0），保持 0.03 游戏秒，再用 0.15 游戏秒快速淡出，无膨胀。总时长 0.18 游戏秒。尺寸沿用旧攻击 4.8 宽度与境界缩放，斜图按对角线长度补偿；命中中心与原效果一致。

验证：针对安装的 RimWorld/Unity DLL 编译通过；XML 解析、两条贴图引用、两张透明通道及与用户选中原图的哈希一致性均通过。实际游戏动画尚未实机观察。
