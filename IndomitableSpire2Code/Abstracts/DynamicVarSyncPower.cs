using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public abstract class DynamicVarSyncPower : IndomitablePower
{
    // 留给子类实现的抽象方法，用于定义如何根据当前层数同步动态变量
    protected abstract void SyncDynamicVars();
    
    // 统一接管正式版的新签名钩子，加入 PlayerChoiceContext 参数
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保触发的是当前能力自身的层数变化
        if (power == this) SyncDynamicVars();
        return Task.CompletedTask;
    }
}