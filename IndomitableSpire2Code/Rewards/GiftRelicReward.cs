using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace IndomitableSpire2.IndomitableSpire2Code.Rewards;

/// <summary>沿用原版遗物图标、说明及跳过按钮；这里的选择仅确认赠品，不直接获取。</summary>
public sealed class GiftRelicReward(RelicModel relic, Player player) : RelicReward(relic, player)
{
    public bool Accepted { get; private set; }
    public bool Skipped { get; private set; }
    
    protected override Task<bool> OnSelect()
    {
        if (Accepted || Skipped) return Task.FromResult(false);
        Accepted = true;
        return Task.FromResult(true);
    }
    
    // 不调用基类：跳过记录与池处理由 CustomRelicCmd 统一执行一次。
    public override void OnSkipped() { if (!Accepted) Skipped = true; }
}