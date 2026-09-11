using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Preparation() : TashkentCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        TashkentKeyword.Choice,
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new ChargeDynamicVar(2M),
        new RetreatDynamicVar(2M)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<ChargePreparation>(base.IsUpgraded),
        HoverTipFactory.FromCard<RetreatPreparation>(base.IsUpgraded)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        List<CardModel> options = [
            base.CombatState.CreateCard<ChargePreparation>(base.Owner),
            base.CombatState.CreateCard<RetreatPreparation>(base.Owner)
        ];
        if (base.IsUpgraded)
        {
            foreach (CardModel option in options)
            {
                CardCmd.Upgrade(option);
            }
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, base.Owner);
        if (selected is KnowledgeDemon.IChoosable choosable)
        {
            await choosable.OnChosen();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Charge"].UpgradeValueBy(1M);
        DynamicVars["TashkentSpire2-Retreat"].UpgradeValueBy(1M);
    }
}
