using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;

public sealed class ImplacableHorn : ImplacableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 沿用 ReaperForm：包含被格挡的伤害与自己的召唤物攻击，逐段触发。
        if (dealer == null || (dealer != Owner.Creature && dealer.PetOwner != Owner) || !props.IsPoweredAttack() || 
            result.TotalDamage <= 0 || !target.IsEnemy || target.IsDead || target.CurrentHp <= 1) return;
        if (await CustomCreatureCmd.ConvertHpToBlock(target, result.TotalDamage) > 0) Flash();
    }
}