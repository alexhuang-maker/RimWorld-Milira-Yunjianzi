# Character Editor 完整角色测试

本 Mod 不需要给 Character Editor 打私有补丁。Character Editor 会按种族读取已加载的 `PawnKindDef`：

- 种族分类：Milira
- PawnKind：`YJZ_Yunjianzi`（显示名“云间子”）

请直接选择该 PawnKind 生成，不要先创建普通 Milira 再逐项组装。生成过程会由 Ariandel Library 一次性应用固定姓名、性别、背景、头型、服装和不可见身份标记，并登记到特殊角色管理器的“米莉拉”分页。

不要启用 `authorizeCheckOnGeneration`：Character Editor 直接生成时没有 SCM 许可，该检查会让 Ariandel Library 跳过固定身份处理。当前模板会在生成后立刻写入姓名、年龄、背景、头型、特性与身份标记；身份标记随 Pawn 一起进入存档，不需要二次注册。

本 PawnKind 没有加入任何派系 PawnGroup、事件、任务或米莉拉帝国的 `UniqueRecruitExtension` 随机招募池，因此正常游戏不会随机生成她。
