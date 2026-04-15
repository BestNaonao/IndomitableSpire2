using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ArmorBreakPower : CustomPowerModel
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
        // 如果是有效攻击且目标确实还有格挡
        if (target == Owner && props.IsPoweredAttack() && target.Block > 0)
        {
            Flash(); // 闪烁能力图标
            // 失去等同于层数的格挡
            await CreatureCmd.LoseBlock(target, Amount);
        }
    }
    
    // 机制补充：在拥有者的回合结束时，层数减少 1
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}