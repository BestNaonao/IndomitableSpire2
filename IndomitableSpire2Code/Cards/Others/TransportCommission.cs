using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class TransportCommission() : CommissionCard(TargetType.Self)
{
    protected override int InitialMaxProgressAmount => 10;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new CardsVar(4)
    ];
    
    // 监听抽牌
    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner == Owner)
            AddProgress(1); // 每抽 1 张牌记录 1 点
        return Task.CompletedTask;
    }
    
    protected override async Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, player);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}