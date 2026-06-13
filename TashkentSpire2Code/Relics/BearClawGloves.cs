using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class BearClawGloves : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/BearClawGloves.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/BearClawGloves.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/BearClawGloves.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>[
        new CardsVar(6)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        ..HoverTipFactory.FromCardWithCardHoverTips<ChargedStrike>(true)
    ];

    public override async Task AfterObtained()
    {
        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 0, base.DynamicVars.Cards.IntValue)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        List<CardTransformation> transformations = (await CardSelectCmd.FromDeckForTransformation(base.Owner, prefs, (CardModel c) => new CardTransformation(c, CreateChargedStrikeFromOriginal(c, forPreview: true)))).Select((CardModel original) => new CardTransformation(original, CreateChargedStrikeFromOriginal(original, forPreview: false))).ToList();
        await CardCmd.Transform(transformations, base.Owner.PlayerRng.Transformations);
    }
    
    private CardModel CreateChargedStrikeFromOriginal(CardModel original, bool forPreview)
    {
        CardModel cardModel = (forPreview ? ModelDb.Card<ChargedStrike>().ToMutable() : base.Owner.RunState.CreateCard<ChargedStrike>(base.Owner));
        if (cardModel.IsUpgradable)
        {
            if (forPreview)
            {
                cardModel.UpgradeInternal();
            }
            else
            {
                CardCmd.Upgrade(cardModel);
            }
        }
        if (original.Enchantment != null)
        {
            EnchantmentModel enchantmentModel = (EnchantmentModel)original.Enchantment.MutableClone();
            if (enchantmentModel.CanEnchant(cardModel))
            {
                if (forPreview)
                {
                    cardModel.EnchantInternal(enchantmentModel, enchantmentModel.Amount);
                    enchantmentModel.ModifyCard();
                }
                else
                {
                    CardCmd.Enchant(enchantmentModel, cardModel, enchantmentModel.Amount);
                }
            }
        }
        return cardModel;
    }
}