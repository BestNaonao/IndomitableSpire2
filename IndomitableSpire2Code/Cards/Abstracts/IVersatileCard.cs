using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

/// <summary>
/// 多用途卡牌契约。母卡保存选择状态，并在打出、升级和牌堆变化时调用共享流程。 同次打出的重放沿用选择；离开打出区后由母卡调用重置指令。
/// </summary>
public interface IVersatileCard
{
    // 实现此接口的母卡自身；应返回 this，供共享逻辑访问卡牌模型。
    CardModel CardModel { get; }
    
    // 母卡保存的临时选项 ID；null 表示未选择。 setter 仅写入状态，不校验或应用效果；切换选项应调用 SelectVersatileOption。
    string? SelectedOptionId { get; set; }
    
    // 多用途的选项列表
    IReadOnlyList<VersatileCardOption> CreateVersatileOptions();
    
    // 选择、清除或重新应用当前选项后，同步母卡的专属状态。相同 ID 也会调用；默认无需额外处理。
    void OnVersatileOptionSelected() { }
}