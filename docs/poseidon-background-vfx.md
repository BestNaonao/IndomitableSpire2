# 海王形态：夜海背景替换（2026-11-07）

## 当前效果

移除原先 `NFormVfx`／FormVfxHolder 下的两层潮汐和六团浪花逻辑，删除旧 `poseidon_form_vfx.tscn`、`poseidon_tide_ring.png`、`poseidon_splash.png` 及其导入／缓存。PNG 可从 Git 恢复。两个导出预设显式排除旧素材和旧场景，避免残留缓存进入包内。

原样复制 `D:/GitClone/AzurLane/data/bg/star_level_bg_598/star_level_bg_598.png` 至 `TashkentSpire2/images/backgrounds/poseidon/star_level_bg_598.png`：1024×576，491,956 字节，SHA-256 `b9ddfd43865abd68960dd859a55e11786226fe1dc9a1e377fc1b8b52d43a8e1c`。素材是月色夜海，不是先前调研中的冰雪背景；以用户本次明确选择为准。

## 结构与生命周期

- `NPoseidonFormVfx` 现在是每个活动战斗一只的 `Control`，不再实例化角色身上的粒子场景；只有首次需要显示海王背景时才创建一个 `TextureRect`。
- 使用 `poseidon_ink_reveal.gdshader`，参考本地 Nagato `nagato_sakura_transition.gdshader` 的固定多尺度噪声、墨边晕染与湿润暗边；展开时间也与当前长门相同：`1.8 / 1.3 ≈ 1.385` 秒。不带长门后半段的樱花、停留或黑屏，不改变夜海图片的最终颜色。
- 夜海图位于 SceneContainer 深度以下 2 层，低于人物及其阴影、高于原背景；普通战斗的原背景整棵子树暂时降层（不重新挂载、不改子节点材质），展开空洞保留正常亮度的原场景，不是全屏透明度交叉淡化。帝皇蟹的处理见下方特例。角色、敌人、手牌、血条、遗物和弹窗不被背景覆盖，所有新增控件忽略输入。
- 控制器 `FullRect` 跟随战斗视口，图片以 TopLevel 的 `KeepAspectCovered` 等比铺满、居中裁切，支持宽屏和窄屏，不跟随 encounter 的相机缩放、偏移和震屏。
- 完全展开后临时隐藏原背景，抑制 Light2D 等残留；移除最后一份形态时沿原遮罩反向消退，期间重新获得形态会连续推进而不重播、不闪烁。原本隐藏的背景保持隐藏。不修改原背景 RGB、透明度、坐标和材质；结束后恢复自身改过的 Z 层级，并保留外部后续颜色／层级修改。
- `PoseidonFormBackgroundPatch` 在活动战斗房间 Ready 后安装控制器；`PoseidonFormPower.AfterApplied`／`AfterRemoved` 立即刷新。每 0.1 秒读取场上玩家已有同步能力，补足场景重建、晚加入节点、死亡和复活；战斗尚未开始不加载图片。
- 多人规则：任一存活玩家有正层数的海王形态即显示同一张背景，叠层／重复施加不重播、不叠图；移除一名玩家的能力不会覆盖其他持有者。怪物持有同名能力不触发。只读同步后的战斗状态，不使用 RNG、不写网络消息、不改能力倍增／每回合触发次数。
- 战斗结束立即隐藏并恢复原场景，不等待淡出；节点退出、场景切换同样恢复和退订。背景节点替换时先恢复旧背景，再接管新背景。事件／预览／已结束战斗房间不安装。
- `AfterCardPlayed` 不再触发旧浪花脉冲；原有 Flash、计数、buff 加值、恢复及抽牌前重置逻辑保留。

## 参考与边界

读取了本地 NagatoSpire2 的 `SakuraFormPower`（中文神子形态）、`sakura_tree_background.tscn`、`NInkStainedSteelSakuraRestSite` 和当前 `nagato_sakura_transition.gdshader`，以及官方 `NCombatRoom`、`NCombatBackground`、`NCombatSceneContainer`。本地神子形态没有运行时背景切换逻辑，因此生命周期为本项目实现，水墨形态和展开速度则参考同项目现有转场。未修改 NagatoSpire2。

## 验证

测试目录：`D:/GitClone/.codex-build/tashkent-poseidon-background-qa-20261107/`。全模组编译通过，仅原有 3 条 CS1998 警告。隔离测试直接链接生产背景控制器，使用真实 Godot 图片、Control 布局、Z 排序与透明度合成；战斗模型为替身，不代表实际双客户端验收。

32 项渲染／生命周期检查通过，最终 stderr 为空：覆盖半程与完整淡入、角色／敌人／手牌 GPU 图层、旧前景隐藏、原有不可见背景保留、3 种宽高比、百次施加只加载一张图、多持有者、死亡／复活、非单位原透明度、后续 RGB 保留、背景替换、怪物排除、结束立即清理、场景退出恢复与订阅释放、已有形态场景重建、非活动房间和 TestMode 跳过。

另用纯资源夹具故意放入两个旧贴图名称和旧场景，采用与正式 BasicExport 相同的排除规则导出 PCK，从空工程挂载验证：1024×576 夜海纹理可用，旧贴图、旧场景及其导入缓存均不存在。该夹具不是完整发布包；没有写入游戏安装目录，尚未进行实际局内／双客户端联机验收。

水墨版隔离测试：`D:/GitClone/.codex-build/tashkent-ink-native-qa-20261107/`。直接链接生产控制器，用 Godot 4.5.1 OpenGL 渲染真正的生产 shader。核验展开四阶段、原色终点、未覆盖区域保持原亮度、敌人／角色／手牌遮挡、正 Z 原前景、视口适配与忽略相机变换、逆向同遮罩、重施加连续推进、外部深度修改保留、多人持有者／死亡复活／场景恢复和结束清理。PNG 中彩色方块仅为演员和 UI 测试替身，不是最终美术。

最终水墨测试 45 项通过，`final.stderr.log` 为空。全模组编译通过，仍只有原有 3 条 CS1998 警告。

## 帝皇蟹背景内演员特例

正式版与 beta 对应的本地原版场景均在 `KaiserCrabBossBackground` 下同时放置风景层和 `KaiserCrab`。后者挂载 `NKaiserCrabBossBackground`，是包含本体、双臂、骨骼节点和附属粒子的实时 Spine 演员，并非可替换风景。原先完整降层并隐藏背景根节点，会连同它一起遮住或隐藏。

现在识别此官方节点，只接管它旁边的风景子层，分别计算包含祖先深度的实际 Z、降到水墨图后方，并在全展开时隐藏风景层。背景根节点及 `KaiserCrab` 整棵子树不修改：父节点、可见性、Z、坐标、缩放、材质、动画及骨骼绑定全部保留；夜海仍正常替换周围风景，不是整场停用海王背景。失去形态、背景替换、战斗结束和节点退出时逐层恢复，只恢复本控制器确实改动的状态，原本隐藏的层不被强制显示。

`tashkent-ink-native-qa-20261107/crab.tscn` 新增 64 项实际 Godot 图层／生命周期检查：四阶段始终保留本体与双臂、海面仍可见、风景隐藏与恢复、官方自行隐藏的 Boss／手臂不被复活、相机变换、重复施加、后续深度修改、替换背景、销毁风景层、结束与退出清理。`crab.stdout.log` 通过，stderr 为空；同时重跑普通战斗的全部 45 项水墨回归，结果通过，`crab-normal-regression.stderr.log` 为空。全模组再次编译通过，0 错误、3 条原有警告。

该专项测试使用与官方相同层级关系的演员／战斗模型替身，实际执行生产控制器、Godot 渲染和生产水墨 shader；没有运行游戏专用 Spine 扩展，因此不能代替实机 Boss 动作和双客户端验收。本次仅修改 C# 和说明文档，不需要新增贴图或改变现有 PCK 资源，未部署至游戏目录。
