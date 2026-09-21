using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CustomCardPileExtensions
{
    private static readonly IComparer<CardModel> DisplayComparer = Comparer<CardModel>.Create(CompareForDisplay);
    
    /// <summary>
    /// 按 piles 顺序收集卡牌，默认按原版查看抽牌堆的规则排序抽牌堆，不混合牌堆。
    /// 返回独立的展示列表，不修改实际牌堆。
    /// </summary>
    public static IReadOnlyList<CardModel> GetCards(this Player player, params PileType[] piles) => 
        player.GetCards(sortDrawPile: true, mixPiles: false, piles);
    
    /// <summary>
    /// 按 piles 顺序收集卡牌，自行指定是否排序抽牌堆，不混合牌堆。
    /// </summary>
    public static IReadOnlyList<CardModel> GetCards(this Player player, bool sortDrawPile, params PileType[] piles) => 
        player.GetCards(sortDrawPile, mixPiles: false, piles);
    
    /// <summary>
    /// 收集卡牌；启用混合时，将所有牌堆的卡牌按稀有度、卡牌 ID 统一排序。
    /// </summary>
    public static IReadOnlyList<CardModel> GetCards(
        this Player player, bool sortDrawPile, bool mixPiles, params PileType[] piles) => 
        player.GetCards(sortDrawPile, mixPiles, CompareForDisplay, piles);
    
    /// <summary>
    /// 先按 piles 顺序收集卡牌，并可独立排序抽牌堆；再按需混合重排。
    /// 所有排序均稳定：比较结果相同的卡牌保留此前的相对顺序。
    /// </summary>
    /// <param name="player">牌堆所属玩家。</param>
    /// <param name="sortDrawPile">是否按原版查看抽牌堆的规则（稀有度、卡牌 ID）排序抽牌堆。</param>
    /// <param name="mixPiles">false 时逐堆拼接；true 时对合并结果调用 comparison 排序。</param>
    /// <param name="comparison">混合后的比较函数；mixPiles 为 false 时不调用。</param>
    /// <param name="piles">要收集的牌堆，顺序决定混合前各牌堆的排列顺序。</param>
    public static IReadOnlyList<CardModel> GetCards(
        this Player player,
        bool sortDrawPile,
        bool mixPiles,
        Comparison<CardModel> comparison,
        params PileType[] piles)
    {
        var cards = new List<CardModel>();
        foreach (var pile in piles)
        {
            var pileCards = pile.GetPile(player).Cards;
            cards.AddRange(sortDrawPile && pile == PileType.Draw
                ? pileCards.OrderBy(card => card, DisplayComparer)
                : pileCards);
        }
        return mixPiles 
            ? cards.OrderBy(card => card, Comparer<CardModel>.Create(comparison)).ToArray() 
            : cards;
    }
    
    private static int CompareForDisplay(CardModel a, CardModel b) => a.Rarity != b.Rarity 
        ? a.Rarity.CompareTo(b.Rarity) 
        : string.Compare(a.Id.Entry, b.Id.Entry, StringComparison.Ordinal);
}