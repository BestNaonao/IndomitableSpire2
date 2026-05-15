using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(TokenCardPool))]
public sealed class EnhancedApAmmo() : IndomitableSpire2Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<ArmorPiercingEnchantment>();
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardsToSelect = DynamicVars.Cards.IntValue;
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, cardsToSelect, cardsToSelect);
        
        var enchantment = ModelDb.Enchantment<ArmorPiercingEnchantment>();
        
        foreach (var card in await CustomCardSelectCmd.FromCombatForEnchantment(
                     choiceContext,
                     Owner,
                     enchantment,
                     1, // 穿甲附魔是无层数的，随便传个数
                     prefs,
                     null, 
                     PileType.Hand, PileType.Draw)
                 )
        {
            CardCmd.Enchant<ArmorPiercingEnchantment>(card, 1M);
            CardCmd.Preview(card);
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级：选择的卡牌数量 +1
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}