# 验证结果（2026-09-18）

- Pack.ps1 / Check.ps1 通过：512 方形 RGBA、严格正面镜像、既有 y=146 分层、侧面镜像、透明后层与原生占位图；renderer 与 Apparel Def 的哈希相较 before 完全一致。
- 已检查 FourDirections_QA.png：长弧线减少反复回折，中央拱起、角色头身区域留空。
- XianxuParticleChecks：4092 项断言通过，含镜像、路径范围、遮挡、暂停确定性与长存档时钟。
- Release 编译成功，0 错误；NU1900 为无法访问 NuGet 漏洞数据服务的网络警告。
- 已覆盖同步到 C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\米莉拉私人拓展—云间子，264 个运行文件 SHA256 一致。
- 未进入游戏实测；需要重新启动游戏加载新纹理和程序集。
