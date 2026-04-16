using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Barracuda831Squadron() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 精锐中队，面临更猛烈的防空火力
    protected override int MaxDurability { get; set; } = 6;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.DiveBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.DiveBomber];
    
    // 仅需破甲提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ArmorBreakPower>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(12M, ValueProp.Move)
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var hasBlock = cardPlay.Target.Block > 0;
        
        // 利用 ModifyDamageMultiplicative 钩子函数修改伤害，就不用在打出逻辑里写双倍伤害了。
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(hasBlock ? "vfx/vfx_heavy_blunt" : "vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        if (cardPlay.Target is not { IsAlive: true }) return attackCmd.Results;
        
        // 提取最终造成的面板伤害总和，根据是否翻倍来逆推单倍伤害
        var totalDealt = attackCmd.Results.Sum(r => r.TotalDamage + r.OverkillDamage);
        var armorBreakStacks = (int)Math.Max(0M, hasBlock ? totalDealt / 2M : totalDealt);
        if (armorBreakStacks > 0)
            await PowerCmd.Apply<ArmorBreakPower>(
                target: cardPlay.Target,
                amount: armorBreakStacks,
                applier: Owner.Creature,
                cardSource: this
            );
        
        return attackCmd.Results;
    }
    
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target is not { Block: > 0 } || dealer != Owner.Creature || cardSource != this || !props.IsPoweredAttack())
            return 1M;
        // 如果对方有格挡，直接将伤害的乘数翻倍
        return 2M;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4M);
        UpgradeDurability();
    }
}