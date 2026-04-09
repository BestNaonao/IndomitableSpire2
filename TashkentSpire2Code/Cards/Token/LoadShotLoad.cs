using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TashkentSpire2.TashkentSpire2Code.Cards.Basics;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class LoadShotLoad() : TashkentCard(1, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AmmunitionDynamicVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = new LoadShot();
        if (IsUpgraded)
            CardCmd.Upgrade(card);
        card.Ammu = (int)DynamicVars[AmmunitionDynamicVar.Key].BaseValue;

        var added = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true);
        CardCmd.PreviewCardPileAdd(added, 2.2f);
    }
    
    protected override void OnUpgrade() =>
        DynamicVars[AmmunitionDynamicVar.Key].UpgradeValueBy(1M);
}