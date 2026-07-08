using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Powers;
using TashkentSpire2.TashkentSpire2Code.Tags;

namespace TashkentSpire2.TashkentSpire2Code.Cards;

public interface IAmmunitionCard
{
    int CurrentAmmu { get; set; }
    int MaxAmmu { get; }
    void UpdateAmmuGlobal(int newValue);
}

public abstract class AmmunitionCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target
) : TashkentCard(baseCost, type, rarity, target), IAmmunitionCard
{
    protected override HashSet<CardTag> CanonicalTags => [TashkentTags.Ammunition];
    
    private int _currentAmmu = -1;

    [SavedProperty]
    public int CurrentAmmu
    {
        get
        {
            if (_currentAmmu == -1 &&
                DynamicVars.TryGetValue("TashkentSpire2-Ammu", out var ammuVar))
            {
                _currentAmmu = (int)ammuVar.BaseValue;
            }

            return _currentAmmu;
        }
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
    
    public int MaxAmmu => DynamicVars.ContainsKey("TashkentSpire2-Ammu-Max") 
        ? DynamicVars["TashkentSpire2-Ammu-Max"].IntValue 
        : 99;
    
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
        int clampedValue = Math.Clamp(newValue, 0, this.MaxAmmu);
        this.CurrentAmmu = clampedValue;

        if (base.DeckVersion is IAmmunitionCard masterCard)
        {
            masterCard.CurrentAmmu = clampedValue;
        }
    }
    
    protected async Task TryTriggerShotEffectAsync(int shellsLoaded, Func<Task> effectAction)
    {
        if (DynamicVars.TryGetValue("TashkentSpire2-Shot", out var shotVar) && shellsLoaded >= shotVar.BaseValue)
        {
            await effectAction();
        }
    }
    
    protected async Task LoadAfterShotAsync(PlayerChoiceContext choiceContext, int shellsLoaded)
    {
        if (DynamicVars.TryGetValue("TashkentSpire2-Shot", out var shotVar) && shellsLoaded >= shotVar.BaseValue)
        {
            int loadAmount = (int)(Owner?.Creature.GetPower<BarrelModificationLoadPower>()?.Amount ?? 0m);

            if (loadAmount > 0 && Owner?.Creature != null)
            {
                await Loadcmd.Execute(choiceContext, this, loadAmount);
            }
        }
    }
    
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (DynamicVars.TryGetValue("TashkentSpire2-Shot", out var shotVar))
            {
                return CurrentAmmu >= shotVar.IntValue;
            }

            return false;
        }
    }
}