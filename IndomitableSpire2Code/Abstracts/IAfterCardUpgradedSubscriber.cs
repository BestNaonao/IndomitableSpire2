using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// CardCmd.Upgrade 实际提升一层升级等级后触发，包含牌组和战斗中的监听者。
/// 原指令为同步方法，本通知也同步完成；不能在此启动需要等待的选牌或动画流程。
/// </summary>
public interface IAfterCardUpgradedSubscriber
{
    public void AfterCardUpgraded(CardModel card);
}