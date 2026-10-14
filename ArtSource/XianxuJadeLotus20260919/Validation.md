# 青莲和顶端显露验证

- 根据用户实机截图撤销负深度上段：整张仙絮保持0.285绘制高度，避免与翅膀深度平面相交。通过顶部抬高0.30×动态尺度、侧向后移和肩部平滑过渡表达脑后位置。
- 参考模组Milira Race的PawnRenderTreeDefs.xml左右翼baseLayer=60；RimSage的PawnRenderUtility.AltitudeForLayer对应约0.022高度。
- 内置imagegen生成常态青莲正面、侧面。运行时9个Normal贴图均在仓库，保留源图及修改前备份。雷光贴图保持原样。
- Check.ps1通过：两套512×512 RGBA、正面严格镜像、东西镜像、透明头身区域。人工查看Forms_Geometry_QA.png确认完整莲冠和肩带连续性。
- 9722项几何/粒子测试通过；编译0错误，NU1900为NuGet漏洞数据网络不可用警告；XML和图标路径检查通过。
- 已部署273个运行文件至C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\米莉拉私人拓展—云间子，SHA256一致。
- 预览只使用运行时位置公式，不模拟人物遮挡，不是游戏截图。尚未在游戏内观察当前修正版；重启后需确认四方向、征召切换与展开翅膀时的显示。
