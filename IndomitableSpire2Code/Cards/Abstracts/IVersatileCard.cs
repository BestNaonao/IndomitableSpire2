namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

/// <summary>打出时选择效果；重放沿用选择，离开打出区后清除。选项只改变母卡的临时形态。</summary>
public interface IVersatileCard
{
    // 所选选项的 ID
    string? SelectedOptionId { get; }
    
    // 多用途的选项列表
    IReadOnlyList<VersatileCardOption> CreateVersatileOptions();
    
    // 选择选项的方法
    void SelectVersatileOption(string? optionId);
}