using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class SwordfishBomber() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IAfterCardUpgradedSubscriber
{
    private int _increasedDamage;
    
    // 只保存永久增量。读档先恢复增量，再由原版重放升级，避免重复计入自身升级的 +3 伤害。
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            var increase = value - _increasedDamage;
            _increasedDamage = value;
            DynamicVars.Damage.BaseValue += increase;
        }
    }
    
    protected override int MaxDurability { get; set; } = 7;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.TorpedoBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    
    // 优雅地继承父类的耐久变量，并追加伤害与进水变量
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(6M, ValueProp.Move),
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
    
    // 牌组其他卡牌升级时永久加 1 伤害。牌组版本和战斗实例都会收到通知，各自增加一次；不再从战斗实例写回牌组版本。
    public void AfterCardUpgraded(CardModel card)
    {
        if (card.Owner != Owner || card.Pile?.Type != PileType.Deck || card == this || card == DeckVersion) return;
        IncreasedDamage++;
    }
    
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += IncreasedDamage;
    }
}