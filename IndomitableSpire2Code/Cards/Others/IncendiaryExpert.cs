using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(TokenCardPool))]
public sealed class IncendiaryExpert() : IndomitableSpire2Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 直接从附魔模型中提取提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<HighExplosiveEnchantment>();
    
    // 注册变量：选择 1 张牌，附魔 2 层高爆
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new("HighExplosiveAmount", 2M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardsToSelect = DynamicVars.Cards.IntValue;
        var amount = DynamicVars["HighExplosiveAmount"].IntValue;
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, cardsToSelect, cardsToSelect);
        
        var enchantment = ModelDb.Enchantment<HighExplosiveEnchantment>();
        
        // 调用我们自定义的战斗内附魔命令，允许选择手牌和抽牌堆
        foreach (var card in await CustomCardSelectCmd.FromCombatForEnchantment(
                     choiceContext,
                     Owner,
                     enchantment,
                     amount,
                     prefs,
                     null, // 高爆附魔自带的 CanEnchantCardType 已经限制了只能选攻击牌
                     PileType.Hand, PileType.Draw)
                 )
        {
            CardCmd.Enchant<HighExplosiveEnchantment>(card, amount);
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardEnchantVfx.Create(card)!);
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级：附魔层数 +1
        DynamicVars["HighExplosiveAmount"].UpgradeValueBy(1M);
    }
}