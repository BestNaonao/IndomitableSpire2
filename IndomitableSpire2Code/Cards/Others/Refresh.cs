using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
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
public sealed class Refresh() : IndomitableSpire2Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    // 与 CardsVar 一起按值复制，记录其中属于闭目的部分；不覆盖升级或其他数值变化。
    private int _restingEyesBonus;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 在悬浮窗中提示能量图标
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(1)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        
        // 2. 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
        // 3. 丢弃 1 张手牌
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1);
        var cardsToDiscard = await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, prefs, null, this);
        foreach (var card in cardsToDiscard)
            await CardCmd.Discard(choiceContext, card);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
    
    public void SyncRestingEyesBonus()
    {
        AssertMutable();
        var bonus = Pile?.IsCombatPile == true && Owner.Creature.GetPowerAmount<RestingEyesPower>() > 0 ? 1 : 0;
        var change = bonus - _restingEyesBonus;
        if (change == 0) return;
        _restingEyesBonus = bonus;
        DynamicVars.Cards.BaseValue += change;
        InvokeExecutionFinished();
    }
    
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        // 复制品可能继承临时加成，却进入没有闭目能力的玩家战斗牌堆。即使场上已经没有闭目能力，也要在入场时自行校正。
        if (card == this) SyncRestingEyesBonus();
        return Task.CompletedTask;
    }
    
    protected override void AfterDowngraded()
    {
        // 原版降级会重建 DynamicVars，旧有的加成标记也必须重置。
        _restingEyesBonus = 0;
        SyncRestingEyesBonus();
    }
}