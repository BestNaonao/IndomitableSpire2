using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class Skua() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string IncreaseKey = "Increase";
    
    // 全金属水密机身，十分耐造
    protected override int MaxDurability { get; set; } = 6;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, IndomitableKeywords.DiveBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.DiveBomber];
    
    // 内部追踪变量，防止降级时清空辛苦攒下的局内成长
    private int _retainedTurns;
    private decimal _extraDamage;
    private decimal _extraWeak;
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(10M, ValueProp.Move),
        new(IncreaseKey, 5M),       // 成长变量，初始为 5
        new CustomPowerVar<WeakPower>(0M),  // 初始 0 层虚弱
        new("RetainedTurns", 0M)    // 专用于向玩家展示的爬升回合数
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 倾泻火力
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
        
        // 尖啸震慑：只有当虚弱层数大于 0 时才施加（因为初始为0）
        if (cardPlay.Target is { IsAlive: true } && DynamicVars.Weak.BaseValue > 0)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext, 
                cardPlay.Target, 
                DynamicVars.Weak.BaseValue, 
                Owner.Creature, 
                this
            );
        }
        return attackCmd.Results;
    }
    
    // 核心机制：检查结算后的真实保留列表
    public override Task AfterFlush(
        PlayerChoiceContext choiceContext, Player player, 
        IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
    {
        // 只要这张牌切实留在了手里（不论是因为什么机制），就开始爬升蓄力！
        if (retainedCards.Contains(this))
        {
            // 伤害与虚弱成长
            var increaseAmount = DynamicVars[IncreaseKey].BaseValue;
            DynamicVars.Damage.BaseValue += increaseAmount;
            _extraDamage += increaseAmount;
            
            DynamicVars.Weak.BaseValue += 1M;
            _extraWeak += 1M;
            
            // 增加并同步展示用回合数
            _retainedTurns += 1;
            DynamicVars["RetainedTurns"].BaseValue = _retainedTurns;
        }
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4M);
        DynamicVars[IncreaseKey].UpgradeValueBy(1M);
        UpgradeDurability();
    }
    
    // 严防降级清零
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += _extraDamage;
        DynamicVars.Weak.BaseValue += _extraWeak;
        DynamicVars["RetainedTurns"].BaseValue = _retainedTurns;
    }
}