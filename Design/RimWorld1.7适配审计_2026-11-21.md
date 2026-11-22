# RimWorld 1.7 适配与兼容审计

审计日期：2026-11-21。对象：云间子本体、云间子建筑扩展及其直接前置/可选兼容模块。不是整套创意工坊订阅清单的兼容认证。

## 环境与迁移依据

本轮开始时 Steam 目录为 1.6.4871；工作期间本机更新为 **1.7.5014 rev1195**。最终构建、加载与运行检查均针对后者，不能把最初的目录检查当作最终环境。

- 新游戏程序集位于游戏根目录：`Assembly-CSharp.dll`、`UnityStubs.dll`、`Waylith.dll`。
- 本机原有 SDK 为 8.0.423。本轮从微软官方下载便携 SDK 10.0.404 到仓库 `.tools/dotnet10`，核验官方 SHA512，不替换系统 SDK。
- [Ludeon 官方 Modder Primer](https://docs.google.com/document/d/e/2PACX-1vSykw5_DYaaw42nEznI9zmBZVT_Vgg2bxYq_v2XWvAsHC3QSKFuDK2OyiQ5WC9jvVRUcrjpFt7xLLF5/pub)说明 1.7 使用 Waylith / .NET 10，并保留 Unity API 兼容层；原生编译需要改引用与目标框架。因此本次保留 `using UnityEngine`，没有无依据地重写所有绘制代码。
- 当前游戏 `RimWorld.runtimeconfig.json` 明确配置 `UnityEngine*=UnityStubs` 重定向。它还包含 Prepatcher 兼容映射；因此不能直接把早期文档中的“Prepatcher 不兼容”当作此版本所有相关模组都不能运行的结论。本模组不依赖 Prepatcher。
- 使用 RimSage 核查了 `PawnCacheRenderer.RenderPawn`、`GenFilePaths`、`ThingIDMaker.GiveIDTo`、`Find.UniqueIDsManager` 和 `Game`。RimSage 未明确索引版本；1.7 特有行为另外用本机真实程序集反编译和运行结果确认。

## 已实施的代码和构建变更

1. 两个工程通过 `tools/RimWorld.Build.props` 按 `RimWorldVersion` 选择框架与引用：1.6 为 net48 / Unity / Harmony 2；1.7 为 net10.0 / UnityStubs / Waylith / Harmony 3。
2. 两个启动入口都显式传入本程序集给 `Harmony.PatchAll`，消除调用栈推断程序集受内联影响的问题。[Harmony 官方源码](https://raw.githubusercontent.com/pardeike/Harmony/master/Harmony/Public/Harmony.cs)直接说明了此风险。
3. 修复建筑工程原有、已不存在的 Harmony `Current/Assemblies` 路径。
4. 本体真实 1.7 编译最初只有一项错误：`Building_YunjianziSwordRainTurret.DrawAt` 调用 Ancot 旧炮塔绘制接口时，`Vector3` 指向旧 `UnityEngine.CoreModule`，出现 CS0012。增加仅供编译的类型转发程序集，把该类型转发到新 `UnityStubs`。保留 Ancot 原有炮塔基类调用，不删除绘制行为，也不修改创意工坊 DLL。
5. 转发程序集位于 `tools/LegacyUnityReferences`，`Private=false`，**不得部署到游戏 Assemblies**。运行时使用游戏自己的重定向机制。
6. 分离 `1.7/Assemblies`、资源和 LoadFolders 路由，保留 1.6 原 DLL/资源。1.6 在本轮没有重新构建，因为 Steam 更新已移除旧游戏程序集；今后若编译 1.6，需另行指定真实 1.6 游戏目录。
7. `tools/Build-YunjianziMods.ps1` 顺序构建本体和建筑扩展；`tools/Deploy-YunjianziMods.ps1` 同步两版本必要运行资源，哈希校验并备份被覆盖文件，不部署源码、SDK、测试项目或编译转发 DLL。
8. 正式/检查构建使用独立中间目录；正式构建禁用增量复用，并核验最终 DLL 不包含检查类型。防止检查版输出被误当成正式版。

## 前置与“只能留在 1.6”的区别

| 对象 | 本机状态 | 判断依据与限制 |
|---|---|---|
| Harmony | 有独立 1.7，0Harmony 3.0.0.0 | 1.7 构建明确引用此版本；不能继续硬绑 1.6 的 Harmony |
| HAR | 声明并提供 1.7 | 完整前置链启动成功；日志显示安装 280 个 HAR 补丁 |
| Ariandel Library | 声明并提供 1.7 | 所用特殊角色窗口类型、方法及字段仍在；激光 Mote 父定义仍在；含 Linux shader bundle |
| Milira Imperium | 声明并提供 1.7 | 可初始化，但一张导弹资源在新引擎解码时报格式错误，见下 |
| Ancot Library | 仅声明至 1.6 | 实际可在 1.7 启动；对本体原生编译存在已确认的旧 Unity 类型身份问题，本轮已增加编译桥接。不能仅凭版本标签判定整个库不能运行 |
| Milira Race | 仅声明至 1.6 | 已参与实际 1.7 完整前置链加载；并非“确定只能 1.6”，但未认证其全部战斗/事件逻辑 |
| Milian Modification | 仅声明至 1.6 | 同上；不能把一次启动验证扩大为整个前置的全功能验证 |
| Facial Animation / Character Editor | 声明至 1.7 | 已在最终联合运行中实际加载；角色编辑操作和所有表情仍需地图验收 |
| Milira Race Gene Patch / Melee Animation | 仅声明至 1.6 | 已在最终联合运行中实际加载；Melee Animation 的 HAR 与 Facial Animation 补丁成功初始化。不能据此认证处决/决斗/套索等全部功能 |

**目前没有证据表明本模组的某项玩法必须永久留在 1.6。** 旧 net48 DLL 和 Unity 引用属于保留的 1.6 构建分支；未声明 1.7 的前置、尚未覆盖的游戏行为应标记为兼容待验证，而不是假装已经全面支持或武断判为不能运行。

## 运行验证方法与边界

使用真实 `RimWorld.exe -NoWindow` 初始化 Vulkan 渲染器；测试资料保存在仓库 `output/rimworld-1.7-audit`，不读取或修改玩家存档、原 ModsConfig。

当前版本 `Verse.ModsConfig.ApplyCommandLineModList` 会忽略不位于本地 Mods 目录的模组，因此最初 `-mod=` 测试漏载了 Workshop 前置。该轮产生的缺类型/缺父定义错误不作为兼容性结论。后续改用独立测试目录里的 ModsConfig，加载实际 Workshop 前置。

运行检查代码在 `tools/RimWorld17Checks/RuntimeChecks.cs`，只有 `-p:YunjianziAudit=true` 才编入，且只有显式 `-YJZCompatibilityAudit` 参数才执行并退出。正式构建移除此代码。

检查覆盖：本体/建筑全部 Harmony 补丁注册、Ancot 炮塔实例及其基类绘制调用、神机激光继承、青/紫/金三色雷电网格提交、RenderTexture 的 Blit/ReadPixels 真正像素读回。像素读回测试覆盖残影/发光蒙版所依赖的底层接口；不等于所有 Pawn 姿态和战斗场景都已目视验收。

两模组 1.7 构建均为 0 警告、0 错误。自有 278 张 PNG/JPEG 的文件头与扩展名均匹配。

最终运行证据：`output/rimworld-1.7-audit/runtime-final/yunjianzi-runtime-checks.txt`。

| 检查 | 实际结果 |
|---|---|
| 完整前置链及四个可选兼容模块 | 19 个模组/官方内容包实际载入，名单写入报告 |
| 本体及建筑的 Harmony 补丁 | 56 个补丁类全部找到对应已安装补丁，包括 Ariandel 特殊角色头像 transpiler |
| Ancot 养剑池 | 实例创建与完整基类 DrawAt 调用通过 |
| Ancot 神机激光 | 实例与继承类型检查通过 |
| 三境界雷电 | 三种材质及 EMP/粒子爆炸网格提交通过 |
| RenderTexture | Blit / ReadPixels 回读的红色像素值检查通过 |

第一次对象检查在主菜单初始化前执行，缺少 `Current.Game` 的 ID 管理器和 `UIRoot.screenshotMode`，产生测试上下文异常。最终测试使用临时 Game，等 UIRoot 初始化后执行并恢复上下文，全部通过；没有为让测试通过而删除游戏绘制逻辑。

## 与本模组分开的已确认问题

- **Milira Imperium**：`Content/Textures/MiliraImperium/Projectile/Missile_Fox3.jpg` 实际文件头为 PNG。Waylith 的 `GpuTextureService.DecodeTextureFile` 实际报错。修改 About.xml 无法修复；应由该前置修正文件扩展名/编码。本轮未修改 Workshop 文件。
- **系统 Vulkan 层**：日志提示 WeGame 的 `CrossVulkanLayer64.json` 文件不存在。本轮测试仍选中 RTX 4060、完成 Vulkan 初始化；未修改注册表或其他应用。
- **已安装模组枚举**：`sleepycot.wingsofdemocracy` 重复 packageId。即便不是本测试启用的模组，枚举阶段也会报错。本轮未删除用户订阅/本地模组。
- 部分前置有 DefOf 初始化时机和英文翻译警告，不等同于本模组移植失败。
- Melee Animation 报告 9 项潜在补丁交叉及 Milira Race 缺少部分武器 tweak 数据。这里只确认它们能联合初始化，没有把这些提示认定为已发生的 1.7 战斗故障，也没有据此宣称全部近战动作都已验证。

## 复现构建

```powershell
./tools/Build-YunjianziMods.ps1 -Version 1.7
./tools/Deploy-YunjianziMods.ps1
```

如需旧版构建：`-Version 1.6 -GameDirectory '<真实1.6目录>'`。不能把 1.7 根目录当作旧 Unity Managed 目录。

后续人工验收仍应覆盖实际地图战斗、保存/读档、人物多朝向残影、盾受击、神机/麒麟死亡与冷却、奥德赛飞船组件。启动和接口检查没有替代这些长期运行场景。

## 最终交付

正式 DLL 已重新构建并核验无 `RimWorld17Checks` 类型；1.7 目录未包含编译用 `UnityEngine.CoreModule.dll`。85 份运行 XML 解析通过。最终程序集哈希见 `output/rimworld-1.7-audit/release-validation.json`。

已同步并逐文件校验：

- `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\米莉拉私人拓展—云间子`：386 个运行文件。
- `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\云间子建筑扩展`：78 个运行文件。

部署记录：`output/rimworld-1.7-audit/deployment-final.json`。测试启动的游戏进程已退出。
