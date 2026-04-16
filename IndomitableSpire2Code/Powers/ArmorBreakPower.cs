using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ArmorBreakPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 使用橙金色作为破甲层数的高亮颜色
    public override Color AmountLabelColor => new("FFB84D"); 
    
    // 核心机制：在承受伤害之前结算破甲
    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext, 
        Creature target, 
        decimal amount, 
        ValueProp props, 
        Creature? dealer, 
        CardModel? cardSource)
    {
        // 确保是该能力拥有者受到伤害，且是正常的攻击伤害
        if (target != Owner || amount <= 0 || !props.IsPoweredAttack())
            return;

        // 如果目标当前拥有格挡，则在受到实质伤害前剥离格挡
        if (target.Block > 0)
        {
            Flash();
            await CreatureCmd.LoseBlock(target, Math.Min(target.Block, Amount));
        }
    }
    
    // 机制补充：在拥有者的回合结束时，层数减少 1
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
            await PowerCmd.TickDownDuration(this);
    }
}