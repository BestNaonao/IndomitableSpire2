# 标记与火控锁定表现

## 行为

- 标记首次获得和实际正向叠层：红色准星由约 1.55 倍收拢至正常大小，主体约 0.42 秒，随后以 16% 透明度持续显示。
- 火控锁定：保留原有给予 7 层／升级 10 层标记的逻辑。开始应用时，在处理区本张卡牌周围展开蓝色雷达，并在指定目标播放强化收拢确认；不扫描或锁定其他敌人。提前到 PowerCmd 的原有等待前启动，避免卡牌结算后立刻离开导致看不到展开过程。
- 火控准星由约 1.85 倍收拢，约 0.9 秒确认后恢复弱显示；雷达按独立的 1.2 秒时长播放，不受卡牌打出／离场时间截断。均不增加 await／逻辑等待。
- 火控确认期间普通叠层不重置或覆盖它，也不会在其结束后补播积压的普通动画。火控牌来源的标记 hook 不额外触发普通动画。
- 标记移除／归零时准星淡出；目标死亡、战斗结束和节点出树时清理。若标记被 Artifact 阻止，火控仍有短暂选中确认，但不会留下常驻标记。
- 目标准星放在角色模型之上、HealthBar／意图等 UI 之下。雷达为处理区卡牌的兄弟节点，放在卡面／文字后方；按卡牌实际中心定位，完整直径约 660～880 点（常规 0.8 倍卡牌约 745 点），比上一版约 642 点稍大。网格 alpha 由 0.72 提高到 0.96，扫描扇区由 0.42 提高到 0.7；均不拦截输入。
- 雷达只跟随对应实例的处理区卡牌；卡牌离开处理区、移入弃牌／消耗区或节点回池复用后，停在最后处理区位置，继续完成剩余扫描和淡出。不可见／远端未展示卡牌时只保留敌人锁定，不退回角色身前。
- 照明弹在对应敌人 Bounds 顶部上方 180 点播放亮白色光核、宽光晕和白色下落光粒；约 2.4 秒自然结束，连续触发复用节点。只在打出时播放，不以常驻能力触发新循环。敌人死亡、战斗结束立即清理。

## 素材来源

四张 PNG 均为本地碧蓝航线资源的直接导出拷贝，没有重新绘制。源根目录：

`C:/Users/15832/Documents/MuMu共享文件夹/AssetBundles`

| 模组文件 | 原包 | 原 Texture2D | 尺寸 | SHA-256 |
| --- | --- | --- | --- | --- |
| `azurlane_mark_reticle.png` | `effect/zhaomingdan_miaozhun` | `miaozhun01` | 256×256 | `70dd7b1eb8d5e7f97c4a6cda7329cffd56024f62a1b0dce755cb952924cafdd2` |
| `azurlane_radar_grid.png` | `artresource/effect/heihailunna/materials/radar01`（`effect/zhongfei_saomiao` 的依赖） | `radar01` | 512×512 | `9ced2b5f65636b3b443e301183d36c290104942bf0e00d7d6765825c549cb82d` |
| `azurlane_radar_sweep.png` | `artresource/effect/heihailunna/gradient01`（`effect/zhongfei_saomiao` 的依赖） | `gradient01` | 512×256 | `da2f8231233d7349e9b5c4b2d5a97f2ba3bdbd1b7925d5fb37be7a4884f3e43c` |
| `azurlane_illumination_glow.png` | `artresource/effect/openbox/al_glow01`（`effect/zhaomingdan_baolu` 的依赖） | `AL_Glow01` | 128×128 | `6b6b6a1e4fb5ba8a9e9e46544823f5c177b1538860c594afb3144f8d37d05c6d` |

雷达扫光通过原生 canvas shader 将渐变纹理重映射为圆形扇区，并采用加法混合，避免黑底和矩形边界。准星／网格保留原图颜色与透明度。

照明弹复用 `zhaomingdan_baolu` 的原始光晕贴图，不是直接运行 Unity prefab。按新的强白光／白色落粒要求重做颜色与运动：一个直径约 260 点的柔光晕、一个直径约 72 点且 RGB 增益 3 的过曝白光核，以及 20 个约 1.05～1.29 秒寿命、向下加速并横向飘散的白色光粒。加法混合不依赖全局 bloom，Compatibility 渲染器也有清晰的亮光表现。固定 22 个共享纹理 sprite，不照搬原 1000 粒子上限和永久循环。

## 实现边界

- `MarkPower.AfterApplied` / `AfterPowerAmountChanged` / `AfterRemoved` 管理通用表现；没有修改标记伤害、层数消耗、反击或回合清除规则。
- `FireControlLockOn.OnPlay` 在原 PowerCmd 调用前启动雷达与目标确认；判定、目标选择及同步仍由原逻辑负责。`IlluminationRound.OnPlay` 只增加非等待的表现调用，不修改标记／照明弹能力数值。
- 每个带标记生物最多一个准星，每张正在结算的卡牌最多一个临时雷达，每名敌人最多一个照明弹特效。叠层和连续触发复用节点；纹理通过游戏资源缓存共享，无粒子发射器。
- 不消耗玩法 RNG、不增加网络消息、不在伤害预览函数生成特效、不按视觉是否存在改变逻辑执行。
- `TashkentCharacter.ExtraAssetPaths` 预热四张贴图与 shader；目标准星使用命中中心，照明弹使用各自模型的 Bounds 顶部。

## 验证

完整模组使用当前安装的游戏程序集编译；隔离 Godot 4.5.1 测试链接生产节点、时间轴、MarkPower 和 FireControlLockOn，模拟官方命令和生物生命周期，检查普通／优先动画、目标归属、连续叠层、Artifact、层数与伤害规则，以及结束清理。

本次扩展到 59 项检查，分别在 Compatibility 与 Mobile 渲染路径通过，包括卡牌中心定位／后方图层／移动不漂移／离开后继续独立扫描／回池复用不跟随／更大范围与透明度、远端无卡牌节点、照明弹原图导入／180 点头顶偏移／亮白光核／宽光晕／白色粒子下落／目标移动／100 次触发复用／自然结束，以及原有两名玩家独立目标、升级层数、伤害和战斗结束清理。隔离测试不等同于实际双机联机实战，也未据此宣称已分别验证所有正式版／Beta 版本。

测试工程及日志：`D:/GitClone/.codex-build/tashkent-mark-vfx-qa`。
