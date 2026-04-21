using MegaCrit.Sts2.Core.Entities.Players;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 自定义钩子接口：当玩家实质性获得能量后触发
/// </summary>
public interface IAfterEnergyGainedSubscriber
{
    public Task AfterEnergyGained(Player player, decimal finalAmount);
}