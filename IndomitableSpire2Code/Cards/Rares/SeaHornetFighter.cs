using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class SeaHornetFighter() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 双发重型战机，拥有极高的初始耐久度
    protected override int MaxDurability { get; set; } = 15;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    // 关键字：战斗机 (加在最前) + 编队 (加在最后)
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.Fighter, IndomitableKeywords.Formation];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.Fighter];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(2M, ValueProp.Move),
        new RepeatVar(8),
        new PowerVar<OnFirePower>(4M)
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            // 推荐搭配激烈的机炮或火箭弹特效
            .WithHitFx("vfx/vfx_fire_burst") 
            .Execute(choiceContext);
        
        if (cardPlay.Target is { IsAlive: true })
        {
            await PowerCmd.Apply<OnFirePower>(
                target: cardPlay.Target,
                amount: DynamicVars["OnFirePower"].BaseValue,
                applier: Owner.Creature,
                cardSource: this,
                silent: true
            );
        }
        
        // 核心机制：呼叫编队，补充手牌
        await CustomCardPileCmd.DrawSameCardAsync(this);
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1M);
        DynamicVars["OnFirePower"].UpgradeValueBy(1M);
        UpgradeDurability();
    }
}