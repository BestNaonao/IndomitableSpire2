# 突进／后撤方向箭头

2026-11-06，将普通距离位移的脚下波浪与沿途泡沫替换为碧蓝航线青色连续箭头。鱼雷飞行泡沫及蓄力打击／致命一击的近身演出不变。

## 方向、颜色与位置

| 角色朝向 | 距离操作 | 实际位移／箭头朝向 | 箭头位置 | 颜色 |
| --- | --- | --- | --- | --- |
| 右 | 突进 | 右 | 角色右前方 | 原图青色 |
| 右 | 后撤 | 左 | 角色左后方 | 红色滤镜 |
| 左 | 突进 | 左 | 角色左前方 | 原图青色 |
| 左 | 后撤 | 右 | 角色右后方 | 红色滤镜 |

`DistancePower` 使用原有、已考虑 Surrounded 朝向的实际 `moveDistance` 决定箭头方向，并单独传递 `delta > 0` 区分突进与后撤；不根据屏幕向左／向右误判动作种类。

箭头位于实际 Bounds 外侧、身体下半部高度，随角色移动，不放在血条或能力下方。三枚 64 世界单位的箭头依次出现（相对最初版本放大两倍），间距同步扩大、向位移方向短滑 28 单位，并在 0.5 秒位移末段淡出。绘制层在身体上方、血条和意图下方，不拦截输入。父节点缩放时仍使用世界坐标计算位置和尺寸。

2026-11-06 后续调整：普通突进／后撤的位移时长由 0.25 秒变成 0.5 秒，保留原有等待与顺序，单次结算多约 0.25 秒，不引入后台位移竞态。移动 Spine 只对当前 move／move_left 的 track entry 使用 0.5 倍速度，不改整个 animation state，后续攻击、受击、待机等动作不受影响。

## 素材

- 原样复制来源：`D:/GitClone/AzurLane/data/artresource/item/bulletall/zidan_zhenliejiantou/zidan_zhenliejiantou_material/zidan_zhenliejiantou.png`。
- 项目资源：`TashkentSpire2/images/vfx/movement/azurlane_direction_chevrons.png`，512×128、27,070 字节。
- SHA-256：`a8d806bdfae78be56cbfe4b883e1e21d414bb4e6eb42d377a4640a4957d3ed85`。
- 依据原 atlas 读取 `guntiao_6` `(226,19,28,28)`、`guntiao_1` `(256,19,27,28)` 和 `guntiao_3` `(285,19,27,28)`，均不旋转；不把整张 atlas 当作序列动画。
- `directional_movement.gdshader` 保留原透明度与明暗，后撤时将原像素亮度重映射为红色，避免直接乘红色导致青色源图变暗；不会修改原始 PNG。

## 移除与打包

已移除项目内旧的 `azurlane_short_wave.png`、`azurlane_movement_foam_4x4.png` 及其 `.import`；清理对应 `.godot/imported` 中的 ctex、md5 缓存。旧 `NWaterFoamVfx.PlayMovement`、位移发射器和短浪绘制分支全部删除；该类只保留鱼雷飞行逻辑。

`BasicExport` 与 `Windows Desktop` 的 `exclude_filter` 均显式增加旧文件名通配符，防止旧资源意外重新导入后进入包中；`BasicExport` 原有的 `ModTemplate.json` 排除规则保留。原始碧蓝航线解包不动，删除的项目素材可从 Git 或原始解包恢复。

## 生命周期与验证

- 每名塔什干最多一个箭头节点，无粒子子节点，三次绘制；连续触发复用，零位移／零时长／TestMode／其他角色不创建。
- 静态模型同样支持；有 Spine 时，之后的任何新动作都能立即中断箭头，包括同名移动重播。死亡、隐藏、场景退出、战斗结束立即清理，退订全部事件。
- 不改变距离数值、位移量、伤害或 Spine 触发；仅普通位移时长与对应移动 entry 调整为半速。无新增 RNG 或网络消息，覆盖本地及远程角色。
- 全模组 Debug 编译通过，仅原有 3 条 CS1998 警告。
- 隔离 Godot 4.5.1 OpenGL + 游戏原生 Spine 扩展，链接生产箭头、鱼雷和飞行泡沫代码，四套真实皮肤 × 左右朝向 × 突进／后撤共 16 个组合通过 GPU 颜色／Bounds 外侧位置检查，总计 146 项检查通过，运行日志无错误。覆盖事件打断、图层、缩放、静态回退、百次变向、死亡／隐藏／结束清理以及鱼雷三段飞行。
- 独立资源打包夹具故意保留旧波浪源图，使用与两个正式预设相同的排除规则导出 PCK，再从空项目挂载检查：新箭头和鱼雷泡沫保留，旧源图及旧导入缓存均不存在。夹具不是完整模组发布包，不等同于完整发布或局内联机验收。

测试目录：`D:/GitClone/.codex-build/tashkent-direction-qa-20261106/`。尚未进行实际单人／多人局内手感验收。

放大与半速后的回归目录：`D:/GitClone/.codex-build/tashkent-direction-revision-qa-20261106/`。共 152 项通过、stderr 为空，保留四套真实皮肤的 16 组合与鱼雷尾迹检查。新增原生 Spine track 半速推进、切换攻击后恢复正常推进、箭头超过旧 0.25 秒后仍存在并在新 0.5 秒期限清理的检查。半速 API 由测试驱动直接调用；生产 `DistancePower` 使用相同 entry API 并经过全项目编译，实际牌序与联机手感仍需局内验收。
