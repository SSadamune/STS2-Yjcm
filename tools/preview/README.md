# 威董卓 Spine 素材交接（历史记录）

2026-10-11 迁移说明：下文的动画统计、采样数据和截图结论来自旧工程交接，不是本次重新渲染结果。旧工程路径已删除，原加工/验证脚本与中间数据可从工作区的单份旧工程 ZIP 恢复至独立临时目录。

当前运行资源位于仓库 `assets/loose/Spine/WeiDongZhuo`，保留的几何工具为 `tools/preview/geometry.mjs`。`node tools/preview/server.mjs` 只提供本地资源服务（首页为本文），不是旧交互播放器；不可按文末旧命令在现行仓库执行。默认 WebGL 库目录为仓库 `node_modules`，可用 `WDZ_NODE_MODULES` 覆盖。现行战斗布局检查入口为 `node tests/check_combat_layout.mjs`。

已按接入约定输出 4.2.11 二进制骨架。大厅使用 Background + Character 两层，同原点、同缩放、同动画名和播放时间，背景先绘制。战斗使用独立 Combat。推荐两处默认 `play`。

所有文件在 `E:\sts2mod\威董卓开发` 内；没有修改 C#、根项目、resource_pack 或发布目录。输入 SKEL/atlas/PNG 和 assets_source 中主代理转换好的 JSON 均未修改。PNG 未重绘、重采样或重新打包；预览由原 Spine 动画程序渲染，非序列帧运行资源。未下载依赖。

## 运行文件

```text
loose_assets/Spine/WeiDongZhuo/
  Character/
    character.skel
    character.atlas
    zhujiemian1_weidongzhuo1.png
  Background/
    background.skel
    background.atlas
    zhujiemian1_weidongzhuo2.png
  Combat/
    character.skel
    character.atlas
    zhujiemian1_weidongzhuo1.png
```

Character 与 Combat 的 PNG 字节完全相同；各自目录下都保留 atlas 原页名。中间 JSON 放在 `spine_preview/output`。没有使用 `--remove-curve`，动画曲线、网格变形、约束和身体槽位均保留。

| 用途 | slots | bones | atlas regions | skin | 动画及秒数 |
| --- | ---: | ---: | ---: | --- | --- |
| Character 大厅原人物层 | 116 | 372 | 101 | default | play 6.67；play2 6.67 |
| Background 场景及马 | 26 | 62 | 23 | default | play 6.67；play2 6.67 |
| Combat 纯人物 | 104 | 372 | 99 | default | play 6.67；play2 6.67 |

运行库浮点时长为 `6.670000076293945` 秒；通常可视作 6.67 秒。不存在 `idle`、`attack` 等动画名。

## 内容层次和裁剪依据

Character 原始顺序（0-based）为：0–1 环境烟雾 `tx/yan1, tx/yan2`；2–6 `pd_*` 服饰飘带；7–15 腿部及 `111/tui_72`；16–44 身体/手臂/武器部件；45 武器叠加光；46–56 披风、毛边与附着叠加光；57–68 手臂和刀；69–105 头部、头发、胡须；106–115 环境光粒 `vx_yg*`。

`pd_1..5` 实际是黄色披风底片及红黄服饰飘带，挂在 `st_18/st_19/pf_37` 等人物骨链；已通过独立渲染核对，因此保留。`pf_8..11` 和 `zs_13` 是身体/武器贴合的叠加光，也保留。没有非人物碎件残留在纯人物渲染中。

Combat 删除原 Character 的 12 个环境槽位：`tx/yan1`、`tx/yan2`、`vx_yg`、`vx_yg2`、`vx_yg3`、`vx_yg4`、`vx_yg5`、`vx_yg6`、`vx_yg7`、`vx_yg8`、`vx_yg9`、`vx_yg10`；对应 skin 和槽位/attachment 时间轴同步移除。atlas 去掉 `tx/yan1`、`vx_yg1` 两个 region，PNG 不动。原人物 JSON 含环境，因此**需要独立 prune，不能直接将大厅 Character 用于纯人物战斗**。

骨骼及索引、全部 bone 时间轴、2 IK / 43 transform constraints 原样保留，避免改变网格权重。Character 中即使有 `ma_*` 命名的遗留骨骼，也没有马的 slot/attachment；不会渲染马。原两套均无 draw-order 时间轴，保留原 setup 绘制顺序；没有像旧 prune_spine.js 那样无条件删除动态排序。

Background 顺序：0–1 `bg_2/bg_1` 场景地面；2–20 `ma_*` 马的部件；21–24 四个 `tx/yan*` 环境烟雾；25 `ys_13` 地面上的刀影，**不是人物手臂**。全部留给大厅。Background 1 IK / 2 transform constraints 保留。

人物的 `tui_7, zs_10, tou_20, tou_19` 是原素材中默认隐藏且两动画不启用的身体替代附件；保留在 skin 中，不会成为屏幕漂浮碎片。Combat 共 100 个槽位在至少一个动画中实际显示。

## 布局坐标

以下都是骨架外部位移 `(0,0)`、loader scale `1` 的世界几何 AABB，按 60 Hz 加全部关键时间及首尾帧采样。坐标精度约 0.001，额外留 5–8% 布局余量。不是碰撞盒，也不是贴图不透明像素的精确边界。包括角色武器和服饰完整摆动。

| 对象 | 动画 | Spine X min..max | Spine Y min..max（向上） | Godot Y min..max（向下） | 宽 × 高 |
| --- | --- | --- | --- | --- | --- |
| Combat | play | -730.189 .. 284.460 | -413.644 .. 412.531 | -412.531 .. 413.644 | 1014.649 × 826.175 |
| Combat | play2 | -739.383 .. 336.539 | -490.829 .. 412.531 | -412.531 .. 490.829 | 1075.922 × 903.360 |
| Background 场景/马，不含烟雾 AABB | play | -1456.948 .. 1331.166 | -760.609 .. 615.475 | -615.475 .. 760.609 | 2788.114 × 1376.084 |
| Background 场景/马，不含烟雾 AABB | play2 | -1481.714 .. 1362.162 | -845.970 .. 615.475 | -615.475 .. 845.970 | 2843.876 × 1461.445 |

Combat 两动画合并的 Godot Rect2：`(-739.383, -412.531, 1075.922, 903.360)`，中心 `(-201.422, 39.149)`。如果只默认 play，其 Rect2 为 `(-730.189, -412.531, 1014.649, 826.175)`，中心 `(-222.865, 0.556)`。大厅两动画场景布局 Rect2 为 `(-1481.714, -615.475, 2843.876, 1461.445)`，中心 `(-59.776, 115.247)`；人物范围位于其中。

两层根骨都是 `(-4702.76,2976.69)`，其后代平移已抵消大部分数值。不要直接把根骨归零，或再按 root offset 做一次补偿。Character 的 `suofang` 缩放 0.23782、Background 的 0.57977 已包含在上述世界边界中；不要额外重复乘这两个比例。

若 Godot Spine 本身已经提供 Y-down 顶点，只使用常规正比例缩放；**不要再翻转一次 Y**。由原始 Y-up 点转换到屏幕的公式：`screenX = originX + scale*spineX; screenY = originY - scale*spineY`。下表 origin 是外部节点的屏幕原点，不是 root bone 的位置。

| 容器 | 推荐 scale | 屏幕原点 (x,y) | 用途 |
| --- | ---: | --- | --- |
| 480 × 480，内边距24 | 0.401516 | (320.874,224.281) | Combat 两动画完整保留 |
| 640 × 600，内边距30 | 0.539073 | (428.581,278.896) | Combat 两动画完整保留 |
| 1280 × 800，内边距32 | 0.427585 | (665.559,350.722) | 大厅两层使用相同值 |
| 1920 × 1080，内边距48 | 0.641378 | (998.339,466.083) | 大厅两层使用相同值 |

这组数值按完整武器/披风包围盒居中，角色躯干本身偏右；若需要让站位标记对准脚底，可在游戏中改用自定义落脚锚点。不要把包围盒中心当作身体中心。

环境特效的大四边形会污染自动 fit：原 Character `play` alpha 可见几何约 3984.763 × 3764.823，`play2` 约 13520.265 × 8112.933；Background 两动画约 9168–9171 × 3678。大厅保留这些特效并按场景/马边界取景，超出画面的烟雾由视口裁切。完整 raw/visible bounds 都在 `output/inspection.json`，不要用其全 FX AABB 把主体缩成小点。

## 动画选择与预览结果

推荐默认 `play`：人物轻微循环，披风/武器附着光及环境光粒活跃。其人物首尾顶点完全一致。`play2` 动作更明显，人物烟雾参与、背景变暗、角色下移；首尾人物顶点最大差 1.130 单位（原 JSON 本来如此），适合作另一动作或表现，不必替换默认。

已用本机 `spine-webgl/core 4.2.119` + 已装 Chrome/Playwright 加载**最终 .skel** 渲染三种组合、两个动画。零 JS/资源/WebGL 错误；0 与 3.335 秒图像均发生变化。没有下载依赖。尚未验证游戏 native Godot 材质/扩展，本次不声称与游戏渲染逐像素相同。

输入 Background PNG 内已有地面拼接的矩形边缘与分块，本预览如实保留；输入人物腿部已有淡化的素材表现，未补绘。PNG 图集的透明区有原始 RGB 扩边数据，保持原样；atlas 无 `pma:true`，WebGL 预览按 straight alpha 上传和绘制。游戏材质交由主代理。

裁剪前后累计比较约 710 万个顶点坐标分量，人物世界顶点和 alpha **完全一致**。转换后的最终二进制与 JSON 动画按相同时间采样，所有层最大顶点差小于 0.001 单位，alpha 差为0。验证证据：`output/asset_validation.json`、`output/render_validation.json`。

## 文件与复现

| 文件 | 内容 |
| --- | --- |
| output/inspection.json | 完整骨骼父子层次、slots、attachment 映射、atlas region 像素边界、每动画 raw/visible AABB |
| output/slot_region_bounds.md | 三套素材逐 slot 和逐 region 的可读表格 |
| output/layout.json | 可直接读取的 Spine/Godot 坐标及缩放/位移预设 |
| output/lobby_play.png、lobby_play2.png | 大厅两层合成透明 PNG，2秒时刻 |
| output/combat_play.png、combat_play2.png | 纯人物透明 PNG，2秒时刻 |
| output/background_play.png、background_play2.png | 独立背景层透明 PNG，2秒时刻 |
| output/costume_ribbons.png | 独立 pd_* 服饰部件核对预览 |
| output/preview_page.png | 独立播放器页面截图 |

从 PowerShell 运行，不需要 npm install：

```powershell
node 'E:\sts2mod\威董卓开发\spine_preview\build_assets.mjs'
node 'E:\sts2mod\威董卓开发\spine_preview\verify_assets.mjs'
node 'E:\sts2mod\威董卓开发\spine_preview\capture.mjs'
node 'E:\sts2mod\威董卓开发\spine_preview\server.mjs'
```

最后一条持续运行本地预览服务，打开 http://127.0.0.1:8793/；可切换大厅/战斗/背景、play/play2，暂停、拖动时间轴、缩放并导出透明 PNG。按 Ctrl+C 停止。capture 脚本使用临时端口并在结束后关闭浏览器与服务。

默认依赖目录为 `E:/sts2mod/_spine_play/node_modules`，可用 `WDZ_NODE_MODULES` 覆盖；Chrome 可用 `WDZ_BROWSER_CHANNEL=msedge` 改成已安装 Edge。构建使用已有 `_spine_conversion_tools/SpineSkeletonDataConverter.exe`，版本参数 `-v 4.2.11`。
