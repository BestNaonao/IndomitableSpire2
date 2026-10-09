using BaseLib.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;

/// <summary>
/// 怨仇的遗物登记池；IsShared 使其进入 ModelDb.AllRelicPools，Ancient 稀有度使其不进入普通商店、宝箱的随机遗物列表。
/// 实际先古奖励选项由 Events.Ancients.Implacable 定义。
/// </summary>
public sealed class ImplacableRelicPool : CustomRelicPoolModel
{
    public override bool IsShared => true;
    public override string EnergyColorName => "colorless";
}