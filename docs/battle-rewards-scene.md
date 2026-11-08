# 塔什干战后奖励场景

## 场景与使用

主场景：`TashkentSpire2/scenes/ui/tashkent_battle_rewards.tscn`。

本机另备有不含 C# 或 Spine 的纯预览项目：`D:/GitClone/.codex-build/tashkent-reward-ui-standalone-20261104/project.godot`，已单独导入并运行验证。若完整模组的旧编辑环境有加载告警，可用此项目直接查看同一份场景快照。当前实际渲染图保存为 `docs/reward-ui-preview/tashkent-battle-rewards-scene-zhs.png`。

展示层由 `tashkent_battle_rewards.gd` 驱动，是独立的 `@tool` Godot Control，不依赖游戏 DLL。可打开场景查看；Inspector 中的 `Preview Language` 可选中、英、日、韩，`Preview Skin` 的 0～4 对应五套现有皮肤，第五套为 μ兵装。预览中的金币／卡牌／药水只是占位内容。

`Preview Safe Top` 默认按 1920×1080 的 180 单位预留顶部区域；运行时使用真实 HUD／遗物栏尺寸，不依赖这个预览占位值。

运行时由 `TashkentBattleRewardsPatch` 与 `NTashkentBattleRewardsController` 装饰原有 `NRewardsScreen`。仅塔什干角色的终结型战斗奖励界面启用；非塔什干、宝箱、事件的非终结奖励和 TestMode 保持原版。

模组设置新增“是否启用模组特殊的战斗结束界面”（`TashkentConfig.EnableCustomBattleRewards`），默认 `true`／是。使用现有 BaseLib 配置保存及恢复默认机制；旧配置缺少该键时保留默认启用。关闭后，新创建的结算屏不安装装饰控制器，保留原版奖励框、继续按钮位置和动画，同时不预加载本界面专用资源。中／英／日／韩均有设置标题与悬停说明。开关在下次创建结算屏时生效，已打开的界面不会被中途替换。

## 保留原版交互

- 左侧直接调整原 `Rewards` 窗的位置和缩放，不重新生成奖励、不改奖池、不修改领取或选牌任务。
- 保留原奖励按钮、滚动遮罩、焦点、继续按钮、热键、跳过限制及多人等待面板。
- 继续按钮只是换无文字 SVG 外观并调整位置。`IsSkip` 和 `UpdateText` 沿用官方；有未领取奖励时仍会显示原本的“跳过”，不要强制显示“继续”。
- 打开选牌页／地图等更高层界面时隐藏新背板；返回奖励界面时恢复。原本的多人等待面板保持位于装饰层上方。
- 不向网络发送新消息，不修改奖励同步器。仅按该奖励集所属玩家的 `CurrentSkin` 选择本地图片。

## 顶部安全区与形成动画（2026-11-04 更新）

针对局内截图中顶部横条／遗物遮住 S 和胜利标题的问题，改为下方 `1920×900` 内容画布，而不是将原来满屏内容直接下移。奖励窗、标题、立绘、名称条和底线重新排版，按可用高度等比缩放；原版继续按钮保留其原生尺寸与悬停缩放，固定在屏幕右下安全边距内。

后续按反馈将背景与内容画布分开：渐变、斜纹、右侧斜切装饰始终铺满整个屏幕，延伸至顶部栏／遗物后方，不再留下纯色空带；只有标题、奖励和立绘等前景继续避让安全区。战利品框先由 X=128 调整至 X=192，本次再右移 48 至 X=240，预览与实际原版奖励框同步，尺寸和领取逻辑不变。

运行时读取 `NRun.GlobalUi.TopBar/BgImage` 的实际底部，以及 `RelicInventory.GetDefaultPosition()` 加第一件遗物的行内位置／高度，换算到奖励屏坐标后另加间隙。按最新要求只避让第一行遗物，第二行及后续行不会推动布局。遗物栏暂时滑出屏幕也不改变第一行常态预留区；整页保留屏幕裁切，动画不采用向上越界的弹跳。

胜利横幅单独上移最多 40 个设计单位，底板上缘至少位于第一行遗物底部再加间隙的位置。通常 1920×1080 下上移约 37～38 单位，奖励框、立绘和名称条不随之移动。白色底板、灰色边线、金色底线已从整页背景绘制中拆成 `VictoryBanner` 原生图形节点；整组底板、S 标识和文字在 `Design` 内排到立绘之后绘制。保持相同画布 Z，不抬高到原版选牌／等待覆盖页上方。第二套皮肤验证图：`docs/reward-ui-preview/tashkent-battle-rewards-banner-skin2-zhs.png`。

动画参考来自本地解包的 `D:/GitClone/AzurLane/data/ui/battleresult_atlas/`，已直接读取 Unity AnimationClip YAML 曲线：

| 官方片段 | 核实到的曲线 | 本场景适配 |
| --- | --- | --- |
| `scale.anim` | 1.5→1 缩放及 0→1 alpha，约 0.333 秒。 | S 标识缩放淡入；为不越过 HUD／奖励边界，收敛为 1.25→1。 |
| `AL_jiesuan_Zi.anim`、`AL_jiesuan_Zi_move.anim` | 大比例标题收拢、约 0.433 秒 alpha，以及约 0.333 秒位置／缩放转换。 | 标题短距离横向滑入与淡入，不做巨大标题盖住奖励或 HUD。 |
| `AL_jiesuan_pingjia.anim` | 0.5～1 秒横向入场，后续分段 alpha。 | 保留分段显现节奏，取消闪烁；立绘与名称条错峰进场。 |
| `AL_jiesuan_guankamingzi.anim`、`AL_jiesuan_dianjijixu.anim` | 名称与继续提示分段 alpha、继续元素横移。 | 名称条、按钮延后短滑入／淡入，底线随后展开。 |
| `enter.anim` | 主区域／评级在 0～1 秒内分段形成。 | 经反馈整体改为半速，总演出约 2.5 秒；仅参考层次与时序，不宣称逐帧复刻 Unity 客户端。 |

当前奖励窗约 0.24～0.84 秒横向滑入并淡入，右侧立绘约 0.24～1.44 秒显现，名称约 1.12～1.84 秒、底线约 1.10～2.20 秒形成。展示层和原生奖励／按钮 Tween 统一采用 `entrance_speed_scale=0.5`，延迟与播放速度同时减半速率，而非只延长最后的停留时间。动画从创建同一帧准备初始状态，不闪现原本居中的奖励框；只替换原 `AfterOverlayShown` 创建的奖励窗视觉 Tween，保留其状态更新逻辑。

没有等待动画完成的任务、延时领取或新增输入遮罩。原版奖励完成／覆盖页隐藏仍控制同一个 Tween，快速领完不会被入场动画重新显示。返回选牌等覆盖页不重播；奖励框仍在入场时发生布局改变则直接完成入场；动画结束后停止 `_process`，没有常驻逐帧布局扫描。

预览：`docs/reward-ui-preview/tashkent-battle-rewards-safe-area-zhs.png` 和 `tashkent-battle-rewards-entrance.webp`。顶部色块只表示 HUD／遗物占用范围，左侧为场景自带的示例奖励，均为生产展示层的 Godot 渲染，不是局内截图。

## 本地化与画风

新增四套 `localization/{zhs,eng,jpn,kor}/gameplay_ui.json`，键以 `TASHKENTSPIRE2-BATTLE_REWARDS.` 为前缀。运行时优先走游戏 `LocString` 合并表，其他语言回退英文。角色名和皮肤名复用原 `characters.json`。

胜利标题使用可编辑 Label、金色渐变、描边与阴影，而非写死中文的“大获全胜”图片。金色 shader 只修改顶点颜色，保留原生字体片元处理，兼容游戏 MSDF 字体；中／日／韩使用游戏 `FontManager` 的对应粗体。场景预览使用系统字体，不额外分发 Windows 字体。

姓名栏不再使用固定 X=1460 的长背景。按当前皮肤测量中／英／日／韩角色名（44 号）和皮肤名（23 号）的实际游戏字体宽度，结果按皮肤缓存；另取英文装饰行的实测宽度及当前语言文字宽度的最大值。右侧增加 32 单位留白及 65 单位斜切边，语言切换不引起四语言之间的宽度跳变。独立预览采用系统字体回退，运行时采用游戏字体，因此预览与局内宽度可能略有差别。

S 标识来自碧蓝航线原资源，仅作胜利装饰，不新增评价条件、经验、好感度或分数。按钮和轮廓为无文字的原生 SVG，因此无需 AI 生成多份语言位图，也不会出现换语言后残留中文的问题。

## 立绘还原

生成后的透明 PNG 位于 `TashkentSpire2/images/ui/battle_rewards/`，最大边不超过 1800 像素；没有使用前一版 AI 样式图作为背景或正式立绘。`Design/Painting` 为固定裁切区域，真正贴图位于其 `Image` 子节点；放大只裁外围背景，不越界到奖励框或顶部遗物栏。

| 皮肤 | 原始来源与还原 |
| --- | --- |
| 默认 | `painting/tashigan_tex` 原始 Texture2D + 已导出 `tashigan-mesh.obj`。 |
| 第二套 | 原始 Sprite 的 render mesh：598 顶点／1731 索引；配合 `paintingface/tashigan_2` 的 0 号表情。脸部采用原 prefab RectTransform 的位置 `(-72, 282.5)` 和尺寸 `302×463`，不是 AI 补脸。 |
| 第三套 | `painting/tashigan_3_tex` 原始 Texture2D + `tashigan_3-mesh.obj`；本次显示倍率增加至 1.18，人物和原有场景一起放大。 |
| 第四套 | 原版确有沙发／窗景／酒桌背景。使用 `tashigan_4_tex` 背景层 + `tashigan_4_rw_tex` 人物层及各自 mesh，输出 `tashigan_4_full_painting.png`（1781×1516）。为补回背景后仍保持人物可读性，显示倍率为 1.25。旧人物单层 PNG 保留，但运行时路径已切换到完整版。 |

第四套组合按原 `painting/tashigan_4` prefab 坐标还原：背景 RectTransform 为 `2992×2520`，原 MeshImage 尺寸 `2048×1725`；中心拉伸的 `layers` 下人物为 `2048×1819`，锚定位置 `(-142,364)`、pivot `(0.490676,0.6665039)`。将各层网格变换到同一坐标后由 Godot 一次离屏渲染成一张贴图，不是把两张裁切图直接居中叠放；局内不增加第二套网格或合成开销。还原脚本为本机 `D:/GitClone/.codex-build/tashkent-reward-ui-20261104/prepare_skin4_full.gd`。

更新后的第三套／第四套预览分别为 `docs/reward-ui-preview/tashkent-battle-rewards-skin3-zhs.png`、`tashkent-battle-rewards-skin4-zhs.png` 和 `tashkent-battle-rewards-skin4-eng.png`。

注意全量解包目录中有同名 Sprite 裁切图与 Texture2D 覆盖：例如默认皮肤 PNG 宽 2038，而原始纹理宽 2048。已从原包定向导出完整 Texture2D，再按 mesh/UV 解码，避免立绘出现接缝。没有改动 `AzurLane/data` 或原始 AssetBundles。

还原脚本与中间日志位于本机 `D:/GitClone/.codex-build/tashkent-reward-ui-20261104/`；使用 AssetStudio 原有解码器与 Godot 网格渲染，不是新画的角色图。

## 验证边界

- 完整 C# 项目编译通过；仅有原项目已有的三条 CS1998 警告。
- 隔离原生 Godot 测试使用生产场景、展示脚本、C# 控制器和真实 Harmony 安装，在 OpenGL／Vulkan 下分别通过 321 项检查；两个最终运行的 stderr 均为空。
- 检查四皮肤、四语言、实际游戏 MSDF 字体覆盖与宽度、1280×720 缩放、原按钮引用／领取回调／键盘焦点、覆盖页隐藏恢复、多人等待层级、语言切换与退订；另外逐帧检查入场不越过第一行遗物安全区、多行遗物不推动布局、中途重新布局、快速领完／返回时不复活奖励窗、继续按钮颜色与原生缩放；并直接读取已安装 `sts2.dll` 核对字段和 getter 存在。
- 新增顶部安全区域背景像素采样、右侧斜切底板延伸、战利品 X=240、统一半速参数、1.5 秒时动画仍在播放及 2.5 秒后停止更新的检查。
- 新增横幅独立上移、整组绘制顺序，以及在立绘层插入不透明探针后的底板像素遮挡检查，避免仅文字抬高而白底仍被立绘覆盖。
- 新增两套皮肤显示倍率／裁切区域、第四套完整版贴图路径、全部四皮肤×四语言姓名栏宽度稳定、三行姓名文字与右侧留白均位于背景内、背景比旧固定宽度更短等检查。
- 配置开关更新后另在 OpenGL 下通过 332 项隔离检查，包含默认启用、四语言设置键、禁用后不创建装饰／保持原版继续按钮位置、重新启用后仅作用于新界面、已打开界面的状态保持一致。BaseLib 的设置 UI／磁盘保存机制仍由原库负责，并非本测试的替身实现所验证的范围。
- 测试中的战斗模型、奖励对象与原始控件行为是替身，不等于实际游戏或双客户端联机测试；真实领取、药水满槽、遗物分配、Boss 换层和联机仍需局内实测。
- 普通 Godot 启动整个模组项目仍可能遇到原有 Spine editor DLL 路径及玩法程序集的加载告警；新展示层已独立为 GDScript，不需要为预览此场景改动 Spine 或其他角色节点。没有擅自修复无关的重复 UID／旧能量场景告警。

本轮没有部署 DLL/PCK 到 Steam 游戏目录。
