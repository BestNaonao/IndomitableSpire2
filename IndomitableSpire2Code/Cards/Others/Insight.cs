using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
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
        var candidates = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw)
            .Where(card => card != this && card.IsUpgradable && (enchantment == null || enchantment.CanEnchant(card)))
            .ToList();
        if (candidates.Count == 0) return;
        
        var maxCards = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, maxCards);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, Owner, prefs)).ToList();
        foreach (var card in selected)
        {
            // 先应用选择时已校验的附魔，避免升级移除消耗等关键词后不再满足附魔条件。
            // CardCmd.Enchant 保留原生的适用范围与同类叠加规则。
            if (enchantment != null)
            {
                var copy = (EnchantmentModel)enchantment.ClonePreservingMutability();
                CardCmd.Enchant(copy, card, copy.Amount);
            }
            CardCmd.Upgrade(card);
        }
        if (selected.Count > 0) CardCmd.Preview(selected);
    }
    
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}