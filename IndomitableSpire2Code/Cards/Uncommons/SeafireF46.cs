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

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class SeafireF46() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 初始耐久极低
    protected override int MaxDurability { get; set; } = 5;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(9M, ValueProp.Move),
        new BlockVar(7M, ValueProp.Move),
        new CustomPowerVar<InterceptedPower>(5M) // 基础截击扣除 3 点力量
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        // 如果目标意图攻击，迎面施加截击削弱其火力，否则掩护获得格挡
        if (cardPlay.Target is { IsAlive: true, Monster.IntendsToAttack: true })
            await PowerCmd.Apply<InterceptedPower>(
                target: cardPlay.Target,
                amount: DynamicVars.Intercepted().BaseValue,
                applier: Owner.Creature,
                cardSource: this
            );
        else
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Block.UpgradeValueBy(3M);
        DynamicVars.Intercepted().UpgradeValueBy(2M);
        UpgradeDurability();
    }
}