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

public sealed class SeaHornetBomber() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    // 高速灵活，结构坚固
    protected override int MaxDurability { get; set; } = 15;
    protected override int UpgradeDurabilityAmount { get; set; } = 5;
    
    // 关键字：水平轰炸机 (最前) + 编队 (最后)
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        IndomitableKeywords.LevelBomber, 
        IndomitableKeywords.Formation
    ];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.LevelBomber];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(6M, ValueProp.Move),
        new RepeatVar(4),
        new PowerVar<OnFirePower>(3M)
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return null;
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(choiceContext);
        
        // 挂载炸弹洗地，对所有存活的被击中敌人附加起火
        await PowerCmd.Apply<OnFirePower>(
            CombatState.HittableEnemies,
            DynamicVars["OnFirePower"].BaseValue,
            Owner.Creature,
            this
        );
        
        // 核心机制：呼叫编队，群蜂出动
        await CustomCardPileCmd.DrawSameCardAsync(this);
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["OnFirePower"].UpgradeValueBy(1M);
        UpgradeDurability();
    }
}