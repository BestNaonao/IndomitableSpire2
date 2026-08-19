using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class DemolitionExpertPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<OnFirePower>(), HoverTipFactory.FromPower<FloodingPower>()];
    
    // 像异蛇头骨一样，附加额外的赋予数值
    // 核心约束：必须是自己施加给敌人，且原始数值 > 0，且属于特定 DOT
    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource) 
        => giver == Owner && target is { IsEnemy: true } && amount > 0M && power is OnFirePower or FloodingPower
            ? Amount : 0M;
    
    // 当引擎最终将这个追加后的数值给出去后，触发动画反馈
    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }
}