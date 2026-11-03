# 战斗胜利动作

- 四套皮肤的 Spine 都包含 `victory` 和 `normal`。本地调研导出的动画长度分别约 3.67、6.67、6.5、7.37 秒。
- `TashkentCharacter.SetupCustomAnimationStates` 注册非循环的 `TashkentVictory` 状态，动作名为 `victory`；由正常动画队列在完成后转入循环 `normal`。若素材没有该动作，不注册状态。
- `TashkentVictoryAnimationPatch` 在 `NCombatUi.OnCombatWon` 的原版奖励流程启动之后，以同步 `void` postfix 向场景内存活的塔什干发出动作触发器。本地和远端玩家均适用；其他角色、宠物、死亡、静态模型、缺少动作或正在回收的节点跳过。
- 没有等待 `finish` 事件、动画完成任务或动画时长，没有新增计时器／网络消息。原版奖励仍只依赖原有的敌人死亡动画等条件，不等待玩家的胜利动作。
- 不订阅新的全局事件，不持有战斗场景引用；场景离开由原有节点生命周期释放动画。

## 验证边界

完整模组使用当前安装的游戏程序集编译成功；当前与 `_107` 官方源码均存在相同的 `NCombatUi.OnCombatWon` 入口。隔离控制台测试链接生产 patch，模拟场景和 Spine 边界，13 项检查覆盖本地／远端、所有跳过条件、回调在动画尚未完成时返回，以及非循环胜利转待机的源码配置。并未以此宣称已经验证四套皮肤的实际游戏播放或双机联机实战。

测试工程：`D:/GitClone/.codex-build/tashkent-victory-qa`。
