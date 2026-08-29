using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PlayerExtensions
{
    /// <summary>
    /// 获取玩家干劲的快捷方法
    /// </summary>
    public static int GetMotivationAmount(this Player player) => 
        player.Creature.GetPower<MotivationPower>()?.DisplayAmount ?? 0;
}