using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Enchantments;

/// <summary>可以用于其他可附魔卡牌；本场战斗打出过的牌在战斗结束时移除其牌组版本。</summary>
public sealed class SampleEnchantment : IndomitableEnchantment
{
    public override bool IsStackable => false;
    public override bool HasExtraCardText => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new PendingRemovalVar()];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    // 参考“黏糊”。AddKeyword 使用集合，甜点原有的消耗不会重复添加；读档时也会重新应用。
    protected override void OnEnchant() => Card.AddKeyword(CardKeyword.Exhaust);
    
    // 参考“迅捷”，但不把 Status 改为 Disabled：试吃每次打出（包括重放）都抽牌。
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status == EnchantmentStatus.Normal)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Card.Owner);
    }
    
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // CombatManager.EndCombatInternal 在此钩子之后才清除 History。
        // 不检查 IsInProgress：原版派发此钩子前已将其设为 false。
        // RunState.IterateHookListeners 也通知牌组本体，所以战斗副本因变化等原因已离开所有牌堆时仍可结算。
        var deckCard = GetDeckCard(Card);
        if (deckCard == null || !WasPlayed(Card)) return;
        
        // 参考“羊毛剪”：保留原版移除钩子、历史记录与卡牌移除动画。
        // 这是附魔自己的效果，不依赖送餐啾仍在玩家身上，也不要求卡牌仍然是一张甜点。
        await CardPileCmd.RemoveFromDeck(deckCard);
    }
    
    // 仅用 DeckVersion 的对象引用定位，绝不按模型 ID 匹配同名牌。第一次移除后本体不在 Deck，重放或多个副本不会重复移除；临时牌没有可移除的本体。
    private static CardModel? GetDeckCard(CardModel card)
    {
        var deckCard = card.Pile?.Type == PileType.Deck ? card : card.DeckVersion;
        return deckCard?.Pile?.Type == PileType.Deck && deckCard.Owner == card.Owner ? deckCard : null;
    }
    
    // 参数 card 能够兼容战斗牌堆中的卡牌和牌组中的原卡牌。
    private static bool WasPlayed(CardModel card)
    {
        var deckCard = GetDeckCard(card);
        return deckCard != null && CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
            entry.CardPlay.Card == card || entry.CardPlay.Card.DeckVersion == deckCard);
    }
    
    // 参考 CalculatedVar 的 IConvertible 扩展点，让原版 LocString 的 cond 格式化器实时查询历史。
    // 不保存“已打出”字段，不依赖刷新顺序；History.Clear 后提示自动消失，克隆也不会遗留标记。
    // 使用 Started：CardModel.Play 真正开始执行时才写入，排队尚未打出的牌不算；
    // 即使打出效果令战斗提前结束，也不必等待 Finished 才认定这张牌已使用。
    private sealed class PendingRemovalVar() : DynamicVar("PendingRemoval", 0)
    {
        protected override decimal GetBaseValueForIConvertible() =>
            _owner is SampleEnchantment { IsCanonical: false, Card: { } card } && WasPlayed(card) ? 1 : 0;
    }
}