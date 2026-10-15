using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
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
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new PlayedThisCombatVar()];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    // 参考“黏糊”。AddKeyword 使用集合，甜点原有的消耗不会重复添加；读档时也会重新应用。
    protected override void OnEnchant() => Card.AddKeyword(CardKeyword.Exhaust);
    
    // 参考“迅捷”，但不把 Status 改为 Disabled：试吃每次打出（包括重放）都抽牌。
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status != EnchantmentStatus.Normal) return;
        // 只标记永久牌组本体。原版在进入 OnPlay 前已记录 CardPlaysStarted；重放会 Upsert 同一效果。战斗副本自身不加红框。
        if (DeckCard is { } deckCard)
            CustomCardPileCmd.UpsertGlow(deckCard, PileType.Deck, CustomCardPileCmd.RedGlowColor);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Card.Owner);
    }
    
    public override Task BeforeCombatStart()
    {
        // 也覆盖重新开始战斗的情况。视觉标记不跨战斗保留，不作为实际移牌的判断依据。
        if (Card.Pile?.Type == PileType.Deck) CustomCardPileCmd.RemoveGlow(Card, PileType.Deck);
        return Task.CompletedTask;
    }
    
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // CombatManager.EndCombatInternal 在此钩子之后才清除 History。
        // 不检查 IsInProgress：原版派发此钩子前已将其设为 false。
        // RunState.IterateHookListeners 也通知牌组本体，所以战斗副本因变化等原因已离开所有牌堆时仍可结算。
        var deckCard = DeckCard;
        if (deckCard == null || !CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
                entry.CardPlay.Card == Card || entry.CardPlay.Card.DeckVersion == deckCard)) return;
        // 移除特效。使用原版移除钩子、历史记录与卡牌移除动画。这是附魔自己的效果，不依赖送餐啾，也不要求卡牌是一张甜点。
        CustomCardPileCmd.RemoveGlow(deckCard, PileType.Deck);
        await CardPileCmd.RemoveFromDeck(deckCard);
    }
    
    // 每次访问都重新定位牌组本体，不缓存已移除的卡牌。仅用 DeckVersion 的对象引用定位，不按模型 ID 匹配同名牌；临时牌没有可移除的本体。
    private CardModel? DeckCard
    {
        get
        {
            var deckCard = Card.Pile?.Type == PileType.Deck ? Card : Card.DeckVersion;
            return deckCard?.Pile?.Type == PileType.Deck && deckCard.Owner == Card.Owner ? deckCard : null;
        }
    }
    
    // 参考 CalculatedVar 的 IConvertible 扩展点，让原版 LocString 的 cond 格式化器实时查询历史。
    // 不保存“已打出”字段。牌组及其预览都不加条件文本；战斗牌堆及其大图按真实来源查询。
    // 这里只检查这一张战斗牌的使用记录，临时生成牌也能显示“已打出过”，不要求存在 DeckVersion。
    // 使用 Started：CardModel.Play 真正开始执行时才写入，排队尚未打出的牌不算；
    // 即使打出效果令战斗提前结束，也不必等待 Finished 才认定这张牌已使用。
    private sealed class PlayedThisCombatVar() : DynamicVar("PlayedThisCombat", 0)
    {
        protected override decimal GetBaseValueForIConvertible()
        {
            if (_owner is not SampleEnchantment { IsCanonical: false, Card: { } card }) return 0;
            var source = CustomCardPileCmd.GetPreviewSource(card);
            return source.Pile?.IsCombatPile == true &&
                CombatManager.Instance.History.CardPlaysStarted.Any(entry => entry.CardPlay.Card == source) ? 1 : 0;
        }
    }
}