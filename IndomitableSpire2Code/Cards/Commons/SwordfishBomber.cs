using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class SwordfishBomber() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 7;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.TorpedoBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    
    // 优雅地继承父类的耐久变量，并追加伤害与进水变量
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(7M, ValueProp.Move),
        new CustomPowerVar<FloodingPower>(2M)
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        // 如果目标承受了鱼雷伤害后仍然存活，则施加进水效果
        if (cardPlay.Target is { IsAlive: true })
        {
            await PowerCmd.Apply<FloodingPower>(
                choiceContext: choiceContext, 
                target: cardPlay.Target,
                amount: DynamicVars.Flooding().BaseValue,
                applier: Owner.Creature,
                cardSource: this
            );
        }
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Flooding().UpgradeValueBy(1M);
        UpgradeDurability();
    }
}