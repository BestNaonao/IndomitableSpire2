using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class SeaHurricane() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 6;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
 
    private decimal _extraDamage;
    private decimal ExtraDamage
    {
        get => _extraDamage;
        set
        {
            AssertMutable();
            _extraDamage = value;
        }
    }
    
    protected override IEnumerable<DynamicVar> AdditionalVars => [new DamageVar(12M, ValueProp.Move)];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        
        return attackCmd.Results;
    }
    
    // 核心机制：当拥有者被攻击时，增加本局游戏内的伤害
    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 确保是真正的攻击，并且检验 CombatState 和所在牌堆以避免误加
        if (target != Owner.Creature || dealer == null || !props.IsPoweredAttack() || 
            CombatState == null || Pile?.Type is PileType.Deck or PileType.None)
            return Task.CompletedTask;
        
        DynamicVars.Damage.BaseValue += 2M;
        ExtraDamage += 2M;
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4M);
        UpgradeDurability();
    }
    
    // 重写降级钩子，确保局内成长不会丢失
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamage;
    }
}