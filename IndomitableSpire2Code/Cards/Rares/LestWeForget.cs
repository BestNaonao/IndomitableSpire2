using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class LestWeForget() : IndomitableCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int BaseDamage = 8;
    private int _currentDamage = BaseDamage;
    private int _increasedDamage;
    
    // 与原版巨镰一致，保存当前伤害与永久增量，供读档和降级后重建数值。
    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = value;
        }
    }
    
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
        }
    }
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationConsumeVar(10M),
        new DamageVar(CurrentDamage, ValueProp.Move),
        new IncreaseVar(2M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];
    
    protected override bool IsPlayable => this.CanAffordMotivationCost();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await this.SpendMotivationCost(choiceContext);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    
    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner || !card.IsCarrierAircraft()) return Task.CompletedTask;
        // 为自身和牌组版本增加伤害变量的值
        var increase = DynamicVars.Increase().IntValue;
        IncreaseDamage(increase);
        (DeckVersion as LestWeForget)?.IncreaseDamage(increase);
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade() => DynamicVars.Increase().UpgradeValueBy(1M);
    
    protected override void AfterDowngraded() => CurrentDamage = BaseDamage + IncreasedDamage;
    
    private void IncreaseDamage(int amount)
    {
        IncreasedDamage += amount;
        CurrentDamage = BaseDamage + IncreasedDamage;
    }
}