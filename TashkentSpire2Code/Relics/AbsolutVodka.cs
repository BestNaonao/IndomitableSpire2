using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;
using TashkentSpire2.TashkentSpire2Code.Enchantment;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class AbsolutVodka : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/AbsolutVodka.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/AbsolutVodka.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/AbsolutVodka.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>[
        new CardsVar(2)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        ..HoverTipFactory.FromEnchantment<FermentEnchantment>(),
        HoverTipFactory.FromCard<Vodka>()
    ];

    public override async Task AfterObtained()
    {
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, base.DynamicVars.Cards.IntValue), player: base.Owner, enchantment: ModelDb.Enchantment<FermentEnchantment>(), amount: 1))
        {
            CardCmd.Enchant<FermentEnchantment>(item, 1m);
        }
    }
}