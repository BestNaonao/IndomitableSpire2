using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(TokenCardPool))]
public sealed class Insight() : IndomitableSpire2Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    // 与原版联网升级次数字段的 8 位范围一致。
    public override int MaxUpgradeLevel => byte.MaxValue;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        
        var enchantment = Enchantment;
        var candidates = Owner.GetCards(sortDrawPile: true, mixPiles: false, PileType.Hand, PileType.Draw)
            .Where(card => card != this && (card.IsUpgradable || enchantment?.CanEnchant(card) == true))
            .ToList();
        if (candidates.Count == 0) return;
        
        var maxCards = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, maxCards);
        var selected = (enchantment == null 
            ? await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner, prefs) 
            : await CustomCardSelectCmd.FromCombatWithEnchantmentInfo(choiceContext, Owner, candidates, enchantment, prefs)
            ).ToList();
        foreach (var card in selected)
        {
            // 两种效果独立判断；不能继承附魔时，仍可升级并保留原附魔。先附魔，避免升级移除消耗等关键词后改变附魔条件。
            if (enchantment?.CanEnchant(card) == true)
            {
                var copy = (EnchantmentModel)enchantment.ClonePreservingMutability();
                CardCmd.Enchant(copy, card, copy.Amount);
            }
            if (card.IsUpgradable) CardCmd.Upgrade(card);
        }
        if (selected.Count > 0) CardCmd.Preview(selected);
    }
    
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}