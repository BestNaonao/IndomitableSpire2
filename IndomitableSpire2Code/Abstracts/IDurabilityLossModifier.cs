using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 耐久变化修改者接口。任何实现了此接口的能力、遗物或卡牌都能拦截并改变舰载机的耐久损失。
/// </summary>
public interface IDurabilityLossModifier
{
    /// <summary>
    /// 尝试在战斗中修改卡牌的耐久度损失。
    /// </summary>
    /// <param name="card">当前结算的卡牌</param>
    /// <param name="originalLoss">原始计算出的防空火力反击损失</param>
    /// <param name="modifiedLoss">修改后的最终损失</param>
    /// <returns>若触发了修改返回 true，否则返回 false</returns>
    bool TryModifyDurabilityLoss(CardModel card, int originalLoss, out int modifiedLoss);
    
    /// <summary>
    /// 当耐久损失被当前修改者成功修改，并在战斗中实际应用后触发（不可用于卡牌预览中）。
    /// </summary>
    Task AfterModifyingDurabilityLoss(CardModel card) => Task.CompletedTask;
}