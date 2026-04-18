using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

public class Vodka() : TashkentCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VigorPower>(3M),
        new CardsVar(1)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    public static async Task<IEnumerable<Vodka>> CreateInHand(Player owner, int amount, CombatState combatState)
    {
        IEnumerable<Vodka> vodkas = Create(owner, amount, combatState);
        await CardPileCmd.AddGeneratedCardsToCombat(vodkas, PileType.Hand, addedByPlayer: true);
        return vodkas;
    }

    public static IEnumerable<Vodka> Create(Player owner, int amount, CombatState combatState)
    {
        List<Vodka> list = new List<Vodka>();
        for (int i = 0; i < amount; i++)
        {
            list.Add(combatState.CreateCard<Vodka>(owner));
        }
        return list;
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(base.Owner.Creature, base.DynamicVars["VigorPower"].IntValue, base.Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, base.Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["VigorPower"].UpgradeValueBy(2M);
    }
}