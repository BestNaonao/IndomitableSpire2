using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class OnsenJourney() : IndomitableCard(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ReconVar(10M),
        new CustomPowerVar<OnFirePower>(2M),
        new CustomPowerVar<FloodingPower>(2M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        if (CombatState == null) return;
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        // 先进行侦查
        var reconCmd = await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
        // 然后根据侦察指令内部记录的选择结果进行过滤和给予负面效果。
        foreach (var card in reconCmd.CardsToHand.TakeWhile(_ => !CombatManager.Instance.IsOverOrEnding && cardPlay.Target.IsAlive))
        {
            // CardsToHand 记录选择结果；满手溢出或被其他效果移走的牌不计入此次成功入手。
            if (card.Pile?.Type != PileType.Hand) continue;
            await PowerCmd.Apply<OnFirePower>(
                choiceContext, cardPlay.Target, DynamicVars.OnFire().BaseValue, Owner.Creature, this);
        }
        foreach (var card in reconCmd.CardsToDiscard.TakeWhile(_ => !CombatManager.Instance.IsOverOrEnding && cardPlay.Target.IsAlive))
        {
            if (card.Pile?.Type != PileType.Discard) continue;
            await PowerCmd.Apply<FloodingPower>(
                choiceContext, cardPlay.Target, DynamicVars.Flooding().BaseValue, Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.OnFire().UpgradeValueBy(1M);
        DynamicVars.Flooding().UpgradeValueBy(1M);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}