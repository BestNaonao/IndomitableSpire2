# 鱼雷白色泡沫与位移短浪

## 行为

- `NTorpedoVfx.LaunchAsync` 开始实际飞行时才创建／激活白色尾迹；生成、悬浮、雷神环绕均不发射泡沫。尾迹从鱼雷后部沿实际经过的路径采样，不画线段，不增加蓝色连线。
- 命中回调首先隐藏并清空尾迹，再完成原飞行任务。雷神访问下一名目标时复用同一个空渲染节点，段间不残留泡沫；最后一击和鱼雷消失时随父节点释放。
- 保留原 0.3 秒飞行、12／24 显示上限、目标顺序、TheBombPower 命中特效及伤害逻辑。
- `DistancePower.UpdateCreaturePositions` 在原位移 tween 启动时，为塔什干本人播放脚下短浪和沿路径留下的白泡沫。方向来自已经考虑 Surrounded 朝向的实际 `moveDistance`，不从 `move`／`move_left` 动作名称推断。皮肤变体和静态模型同样适用，不给宠物额外铺浪。
- 位移仍为原有 0.25 秒，短浪约 0.48 秒消退，泡沫最晚约 0.65 秒清理，不增加任何等待。零位移／TestMode 不生成，死亡和战斗结束立即停止并清理。
- 泡沫保留世界坐标，不随父角色继续平移；短浪跟随实际 Bounds 脚底，位于 HealthBar／意图 UI 后方，不拦截输入。

## 素材与渲染

三张贴图直接复制自本地碧蓝航线解包，未重新绘制或覆盖原文件：

| 模组文件 | 源资源包 | 原图 | SHA-256 |
| --- | --- | --- | --- |
| `azurlane_torpedo_foam_4x4.png` | `artresource/effect/xulie/xulie_penjian001_4x4`，`effect/torpedotail` 的依赖 | 256×256，4×4 | `b126286940897dfa13b929f26d99ffc8749025e9d324439cbffd536e734f380e` |
| `azurlane_movement_foam_4x4.png` | `artresource/effect/movewave/sequence_4x4_03`，`effect/movewave` 的依赖 | 256×256，4×4 | `f8b699373e8fc099886ce6b0e76d384cbf771141ce6dc7f8e655ae86037dc8e8` |
| `azurlane_short_wave.png` | `artresource/effect/movewave/wave`，`effect/danchuanlanghuaxiao` 的依赖 | 512×256 | `e1aef5dcf8807105111271accc4eb0ea14fd742e06ddcb26c30e756a7512b29a` |

共享 `white_water_foam.gdshader` 将泡沫变为白色，并抑制短浪原图中的蓝色水面底层；保留原贴图的透明度。序列从 Unity 的左下角第一格开始，逐格读取完整 16 帧。

`NWaterFoamVfx` 直接绘制固定池，不创建粒子子节点。每个渲染节点最多 24 个泡沫面片，每帧最多新增 12 个路径采样点，低帧率／大位移不会无限补发。鱼雷只有首次飞行时创建一个渲染节点，后续目标复用；每名角色同时最多一个位移渲染节点。纹理和 shader 由角色资源列表预热。不消耗玩法 RNG，不新增网络消息，不改变同步目标或数值结算。

## 连续飞行完成通知

实际 Godot tween 测试复现了原 `CompleteLaunch` 的同步重入问题：`TrySetResult` 可以立即恢复下一段飞行，随后旧方法将新 `_launchCompletion` 清空，导致第二段完成通知丢失。现先将旧完成源取到局部变量并清空字段，再完成旧任务。这样保留下一段新建的完成源，不增加帧等待或改成并行结算。

## 验证

完整模组针对当前游戏程序集编译成功，只有既有的三个 CS1998 警告。隔离 Godot 工程链接生产 `NTorpedoVfx` 和 `NWaterFoamVfx`，使用真实引擎 tween、shader 和导入贴图，模拟生物模型边界。

28 项检查分别在 Compatibility 与 Mobile 渲染路径通过，覆盖：悬浮无尾迹、飞行采样上限、白色 shader、命中立即清空、连续三段飞行与复用、末击回收、左右位移与脚底图层、旧泡沫世界定位、100 次连续变向复用、低帧率采样上限、自然结束、零位移／TestMode、死亡、鱼雷取消、战斗结束完成任务和事件解绑。

测试目录：`D:/GitClone/.codex-build/tashkent-water-qa`。这些是模拟模型边界的隔离验证，不等同于实际四人联机、完整海王／雷神组合或所有正式版／Beta 游戏版本的实测。
