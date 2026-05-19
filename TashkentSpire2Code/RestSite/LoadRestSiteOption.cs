using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.RestSite;

public sealed class LoadRestSiteOption : CustomRestSiteOption
{
    private IEnumerable<CardModel>? _selection;
    
    private const int LOAD_AMOUNT = 6;

    public override string OptionId => "TASHKENTSPIRE2-LOAD";

    public override string CustomIconPath => "res://TashkentSpire2/images/rest_site/load.png";
    
    public LoadRestSiteOption(Player owner) : base(owner)
    {
        this.IsEnabled = owner.Deck.Cards.Any(c => c is IAmmunitionCard);
    }
    
    public override LocString Description
    {
        get
        {
            LocString description;
            if (this.IsEnabled)
            {
                description = new LocString("rest_site_ui", $"OPTION_{this.OptionId}.description");
                description.Add("Count", (decimal)1); 
            }
            else
            {
                description = new LocString("rest_site_ui", $"OPTION_{this.OptionId}.descriptionDisabled");
            }
            return description;
        }
    }

    public override async Task<bool> OnSelect()
    {
        LocString prompt = new LocString("rest_site_ui", $"OPTION_{this.OptionId}.name");

        CardSelectorPrefs prefs = new CardSelectorPrefs(prompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        
        this._selection = await CardSelectCmd_Extensions.FromDeckForLoad(this.Owner, prefs);

        if (this._selection == null || !this._selection.Any())
            return false;

        CardModel targetCard = this._selection.First();
        await Loadcmd.Execute(null, targetCard, LOAD_AMOUNT);

        return true;
    }
}

public static class CardSelectCmd_Extensions
{
    public static Task<IEnumerable<CardModel>> FromDeckForLoad(
        Player player,
        CardSelectorPrefs prefs)
    {
        List<CardModel> deck = PileType.Deck.GetPile(player).Cards.ToList();

        return CardSelectCmd.FromDeckGeneric(
            player, 
            prefs, 
            filter: (CardModel c) => c is IAmmunitionCard,
            sortingOrder: (CardModel c) => deck.IndexOf(c)
        );
    }
}