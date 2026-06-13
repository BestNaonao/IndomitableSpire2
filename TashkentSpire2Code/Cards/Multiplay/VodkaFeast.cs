using BaseLib.Patches.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Multiplay;

public sealed class VodkaFeast() : TashkentCard(3, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Vodka>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        IEnumerable<Creature> enumerable = from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;
        foreach (Creature creature in enumerable)
        {
            if (creature.Player != null)
            {
                int num = MaxHandSizePatch.GetMaxHandSize(creature.Player, MaxHandSizePatch.DefaultMaxHandSize) - CardPile.GetCards(creature.Player, PileType.Hand).Count();
                List<Vodka> cards = Vodka.Create(creature.Player, num, base.CombatState, false).ToList();

                IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, base.Owner);
                if (LocalContext.IsMe(creature))
                {
                    CardCmd.PreviewCardPileAdd(results);
                }
            }
        }
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}