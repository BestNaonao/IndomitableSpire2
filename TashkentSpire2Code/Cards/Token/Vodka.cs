using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Vodka() : TashkentCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VigorPower>(2M),
        new CardsVar(1)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>()
    ];
    
    public static async Task<IEnumerable<Vodka>> CreateInHand(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
    {
        IEnumerable<Vodka> vodkas = Create(owner, amount, combatState, isUpgraded);
        await CardPileCmd.AddGeneratedCardsToCombat(vodkas, PileType.Hand, owner);
        return vodkas;
    }

    public static IEnumerable<Vodka> Create(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
    {
        List<Vodka> list = new List<Vodka>();
        if (combatState != null)
        {
            for (int i = 0; i < amount; i++)
            {
                list.Add(combatState.CreateCard<Vodka>(owner));
            }
            if (isUpgraded)
            {
                foreach (var item in list)
                {
                    CardCmd.Upgrade(item);
                }
            }
        }
        return list;
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, base.DynamicVars["VigorPower"].IntValue, base.Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, base.Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["VigorPower"].UpgradeValueBy(2M);
    }
}