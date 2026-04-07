using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Cards;

public abstract class AmmuCard : TashkentCard
{
    public int Ammu;

    private bool _returnToHandThisPlay;

    protected AmmuCard(int cost, CardType type, CardRarity rarity, TargetType target) : base(cost, type, rarity, target) {
        Ammu = GetInitialAmmo();
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play) {
        _returnToHandThisPlay = false;

        await OnPlayEffect(ctx, play);
        
        if (Ammu > 0) {
            _returnToHandThisPlay = true;
            base.EnergyCost.SetThisTurnOrUntilPlayed(0);
        }
        else {
            await CardCmd.Exhaust(ctx, this);

            TashkentCard card = CreateLordType();

            var added = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, addedByPlayer: true);
            CardCmd.PreviewCardPileAdd(added, 2.2f);
        }
    }

    protected override PileType GetResultPileType() {
        var result = base.GetResultPileType();

        if (result == PileType.Discard && _returnToHandThisPlay)
            return PileType.Hand;

        return result;
    }

    protected abstract Task OnPlayEffect(PlayerChoiceContext ctx, CardPlay play);

    protected abstract int GetInitialAmmo();

    protected abstract TashkentCard CreateLordType();
}