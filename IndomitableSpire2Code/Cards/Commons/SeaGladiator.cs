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

public sealed class SeaGladiator() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 5;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    // 记录本场战斗中同时增加的伤害和格挡，供降级后恢复。
    private decimal _extraDamageAndBlock;
    private decimal ExtraDamageAndBlock
    {
        get => _extraDamageAndBlock;
        set
        {
            AssertMutable();
            _extraDamageAndBlock = value;
        }
    }
    
    public override bool GainsBlock => true;
    
    // 优雅地继承父类的耐久变量，并追加伤害与格挡变量
    protected override IEnumerable<DynamicVar> AdditionalVars =>
        [new DamageVar(4M, ValueProp.Move), new BlockVar(4M, ValueProp.Move)];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 造成伤害
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        return attackCmd.Results;
    }
    
    // 监听拥有者受到攻击，包括被格挡完全抵消的攻击。
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 排除手牌和消耗牌堆，其余战斗牌堆（包括 MOD 自定义牌堆）均可触发。
        if (target != Owner.Creature || dealer == null || !props.IsPoweredAttack() || 
            CombatState == null || Pile?.Type is null or PileType.Hand or PileType.Exhaust) return;
        
        // 每次满足条件时，伤害和格挡各增加 1 点，并返回手牌。
        DynamicVars.Damage.BaseValue += 1M;
        DynamicVars.Block.BaseValue += 1M;
        ExtraDamageAndBlock += 1M;
        await CardPileCmd.Add(this, PileType.Hand);
    }
    
    protected override void OnUpgrade()
    {
        // 升级提升基础数值以及耐久度
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars.Block.UpgradeValueBy(2M);
        UpgradeDurability();
    }
    
    // 重写降级钩子，确保局内成长不会丢失
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamageAndBlock;
        DynamicVars.Block.BaseValue += ExtraDamageAndBlock;
    }
}