using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TashkentSpire2.TashkentSpire2Code.Enchantment;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Test5 : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
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
        ..HoverTipFactory.FromEnchantment<EndeavourEnchantment>()
    ];

    public override async Task AfterObtained()
    {
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, base.DynamicVars.Cards.IntValue), player: base.Owner, enchantment: ModelDb.Enchantment<EndeavourEnchantment>(), amount: 1))
        {
            CardCmd.Enchant<EndeavourEnchantment>(item, 1m);
            NCardEnchantVfx? nCardEnchantVfx = NCardEnchantVfx.Create(item);
            if (nCardEnchantVfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(nCardEnchantVfx);
            }
        }
    }
}