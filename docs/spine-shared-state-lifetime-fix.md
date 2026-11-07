# 蓄力下蹲与待机共享动画状态修复（2026-11-07）

## 原因

用户游戏日志在打出 `CHARGED_STRIKE` 后，从第 588 行开始反复出现 `ObjectDisposedException / Godot.RefCounted`，栈位于 `NTashkentIdleAnimation.CurrentAnimation → MegaAnimationState.GetCurrent`。读取时共 103 条同类异常，另有 41 条记录异常过程中的日志器空引用。日志中未发现水墨背景／shader／新增骨架的加载失败。

实际游戏 Spine 扩展的两次 `get_animation_state` 返回同一个原生包装对象，而不是两份可独立释放的资源。`NCloseStrikeVfx` 新建的 `CrouchAnimationState` 与 `CreatureAnimator`／待机控制器借用同一包装对象；此前在拒绝占用轨道或结束下蹲时调用 `Dispose`，会使待机已缓存的包装对象失效。

## 修复

- 移除下蹲控制器两个共享状态的 `Dispose` 调用。命中／打断／结束只清理自有的 track 1，并丢弃自己的托管引用，不释放整份状态、不影响基础攻击和别人的附加轨道。
- 待机控制器在现有每秒 5 次轮询中检查 sprite 和 state 包装是否仍有效；失效时重新获取借用状态，未就绪或 sprite 已销毁时暂停计时。`CurrentAnimation` 和活动重置走相同保护路径，不靠捕获异常来掩盖错误。
- 不改变下蹲姿态／时长、伤害／联机结算、10 秒待机门槛、致命一击回跳或水墨背景，不新增素材或网络消息。

## 验证方式

隔离工程：`D:/GitClone/.codex-build/tashkent-spine-lifetime-qa-20261107/`。

直接链接生产 `NCloseStrikeVfx`、`NTashkentIdleAnimation` 和 `TashkentIdleClock`；此次 Spine C# 类型、借用状态、track 包装与 ready／树帮助方法使用实际安装的 `sts2.dll`，原生扩展使用游戏 DLL。战斗模型／命令接口仍为替身，不代表实际双客户端验收。

联合覆盖四皮肤的原生共享对象身份、下蹲期间待机轮询、命中后持续读取原缓存状态、10 秒后正常待机、活动取消、受击／胜利／死亡／移动／同名攻击打断、忙轨道拒绝、百次连续重播、目标失效、战斗结束、外部错误释放后的防御性恢复、角色选择预览，以及原有四皮肤双朝向的脚位／比例／返回动画回归。

负对照只在隔离副本重加原先两个错误的 `Dispose`：同一测试在第一个“命中后原待机缓存仍有效”断言立即失败。证明测试可捕捉此共享生命周期错误，不只是靠待机防御性重取把它隐藏。生产文件不含这些负对照调用。

修复后全模组构建通过，仍仅原有 3 条 CS1998 警告。产物写到隔离构建目录，未替换正在运行游戏中的 DLL。本次只涉及代码，无需重新生成美术资源或 PCK。

最终修复版 `final.stdout.log` 共 1,681 项通过，`final.stderr.log` 为空；包括四套皮肤的 sprite 销毁后安全暂停。负对照 `negative.stderr.log` 在预期的共享缓存有效性断言处失败。编译后的 DLL 位于 `D:/GitClone/.codex-build/tashkent-mark-vfx-qa/full-build/TashkentSpire2.dll`，SHA-256：`8e3642a38f13df62876aa9cd75d87938caa21e66ce42489ce1fe7c7d54b3478f`。关闭游戏后替换模组同名 DLL 并重新启动生效；未宣称已完成实际局内／双客户端验收。
