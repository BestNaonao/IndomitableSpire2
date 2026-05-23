using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CombatStateExtensions
{
    /// <summary>
    /// 批量创建指定类型的卡牌实例（仅创建，不加入任何牌堆）
    /// </summary>
    public static IEnumerable<TCard> CreateCards<TCard>(
        this CombatState combatState, 
        Player player, 
        int amount, 
        bool isUpgraded)
        where TCard : CardModel
    {
        var list = new List<TCard>();
        for (var i = 0; i < amount; i++)
        {
            var card = combatState.CreateCard<TCard>(player);
            if (isUpgraded) CardCmd.Upgrade(card);
            list.Add(card);
        }
        return list;
    }
}