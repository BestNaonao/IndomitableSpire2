using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class VaseOfHazyAroma : IndomitableRelic
{
    private int _tokenCardsPlayed;
    
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4), new HealVar(3)];
    
    public override bool ShowCounter => true;
    
    public override int DisplayAmount => TokenCardsPlayed;
    
    [SavedProperty]
    public int TokenCardsPlayed
    {
        get => _tokenCardsPlayed;
        private set
        {
            AssertMutable();
            _tokenCardsPlayed = value % DynamicVars.Cards.IntValue;
            Status = _tokenCardsPlayed == DynamicVars.Cards.IntValue - 1 ? RelicStatus.Active : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }
    
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner || cardPlay.Card.Rarity != CardRarity.Token)
            return;
        TokenCardsPlayed++;
        if (TokenCardsPlayed != 0)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}