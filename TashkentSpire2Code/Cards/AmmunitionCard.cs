using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TashkentSpire2.TashkentSpire2Code.Cards;

public interface IAmmunitionCard
{
    int CurrentAmmu { get; set; }
    void UpdateAmmuGlobal(int newValue);
}

public abstract class AmmunitionCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target
) : TashkentCard(baseCost, type, rarity, target), IAmmunitionCard
{
    private int _currentAmmu = -1;

    [SavedProperty]
    public int CurrentAmmu
    {
        get => _currentAmmu;
        set
        {
            AssertMutable();
            _currentAmmu = value;
            if (DynamicVars.TryGetValue("TashkentSpire2-Ammu", out var ammuVar))
            {
                ammuVar.BaseValue = value;
            }
        }
    }
    
    public override void AfterCreated()
    {
        base.AfterCreated();

        if (_currentAmmu == -1 && DynamicVars.TryGetValue("TashkentSpire2-Ammu", out var ammuVar))
        {
            _currentAmmu = (int)ammuVar.BaseValue;
        }
    }

    public void UpdateAmmuGlobal(int newValue)
    {
        int max = DynamicVars.ContainsKey("TashkentSpire2-Ammu-Max") 
            ? DynamicVars["TashkentSpire2-Ammu-Max"].IntValue 
            : 99;
            
        int clampedValue = Math.Clamp(newValue, 0, max);
        this.CurrentAmmu = clampedValue;

        if (base.DeckVersion is IAmmunitionCard masterCard)
        {
            masterCard.CurrentAmmu = clampedValue;
        }
    }

    protected abstract Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu);
    protected abstract Task OnPlayWithoutAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CurrentAmmu > 0)
        {
            await OnPlayWithAmmu(choiceContext, cardPlay, CurrentAmmu);
        }
        else
        {
            await OnPlayWithoutAmmu(choiceContext, cardPlay);
        }
    }
}