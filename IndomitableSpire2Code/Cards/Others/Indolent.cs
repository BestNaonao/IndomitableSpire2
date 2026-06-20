using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(StatusCardPool))]
public sealed class Indolent() : IndomitableSpire2Card(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    // 状态牌通常不能升级
    public override int MaxUpgradeLevel => 0;
    
    // 关键字：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 注册失去干劲的变量，供文本调用
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MotivationConsumeVar(10M)];
    
    // 核心机制 1：抽到时失去干劲
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await Cmd.Wait(0.25f);
        await PowerCmd.Apply<MotivationPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: -DynamicVars.MotivationConsume().BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    // 核心机制 2：打出时选择一张牌丢弃
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 调用原版的弃牌选择器文本
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1);
        // 弹出弃牌界面
        var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, prefs, null, this);
        // 执行丢弃
        foreach (var card in cardsToDiscard)
            await CardCmd.Discard(choiceContext, card);
    }
}