using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class CombatWraith() : TashkentCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int _intangibleThreshold = 9;

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            PlayerCombatState? playerCombatState = base.Owner.PlayerCombatState;
            if (playerCombatState == null)
            {
                return false;
            }
            return playerCombatState.Hand.Cards.Count > 9 || playerCombatState.Hand.Cards.Count <= 1;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(14M, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        List<CardModel> list = base.Owner.PlayerCombatState!.Hand.Cards.ToList();
        int exhaustedCount = 0;
        foreach (CardModel item in list)
        {
            await CardCmd.Exhaust(choiceContext, item);
            exhaustedCount++;
        }
        if (exhaustedCount >= 9 || exhaustedCount == 0)
        {
            await PowerCmd.Apply<IntangiblePower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}