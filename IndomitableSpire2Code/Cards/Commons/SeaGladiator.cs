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
    
    // 用于记录上一次触发起飞的回合数，防止多段攻击或多名敌人造成的无限回手
    private int _lastTriggerRound = -1;
    
    // 添加专门记录局内成长格挡值的内部变量
    private decimal _extraBlock;
    private decimal ExtraBlock
    {
        get => _extraBlock;
        set
        {
            AssertMutable();
            _extraBlock = value;
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
    
    // 核心拦截：监听拥有者受到伤害
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 1. 安全校验：必须是本角色受到伤害，且必须是敌人的真实“攻击”
        // 2. 频率控制：每回合（大回合）仅限触发一次
        // 3. 状态校验：这张牌必须在弃牌堆或抽牌堆
        if (target != Owner.Creature || dealer == null || !props.IsPoweredAttack() || 
            CombatState == null || CombatState.RoundNumber <= _lastTriggerRound || 
            Pile?.Type is not (PileType.Discard or PileType.Draw)) return;
        
        // 记录本回合已触发，局内格挡值 +1，并且返回手牌
        _lastTriggerRound = CombatState.RoundNumber;
        DynamicVars.Block.BaseValue += 1M;
        ExtraBlock += 1M;
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
        DynamicVars.Block.BaseValue += ExtraBlock;
    }
}