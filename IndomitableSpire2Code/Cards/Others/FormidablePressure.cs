using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(ColorlessCardPool))]
public sealed class FormidablePressure() : IndomitableSpire2Card(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CalculationBaseVar(12M), 
        new ExtraDamageVar(6M), 
        // 预览与实际伤害都使用本次传入的目标；未选择目标时只计算基础伤害。
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, target) => 
            target == null ? 0 : CombatManager.Instance.History.Entries
                .OfType<PowerReceivedEntry>()
                .Count(entry => entry.Actor == target && entry.HappenedThisTurn(card.CombatState) &&
                    // 按施加时的变化量判定，排除抵消/衰减，并计入负力量等负数施加。
                    entry.Amount != 0M && entry.Power.GetTypeForAmount(entry.Amount) == PowerType.Debuff))
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4M);
        DynamicVars.ExtraDamage.UpgradeValueBy(3M);
    }
}