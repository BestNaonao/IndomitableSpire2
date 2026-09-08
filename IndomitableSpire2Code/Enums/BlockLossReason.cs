namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

/// <summary>
/// <c>CreatureCmd.LoseBlock</c> 的语义来源。
/// </summary>
public enum BlockLossReason
{
    /// <summary>普通游戏效果造成的格挡损失。</summary>
    Normal,
    
    /// <summary>为落实跨回合格挡保留上限而进行的维护性裁剪。</summary>
    RetentionAdjustment,
    
    /// <summary>其他明确不应按普通损失处理的维护性来源。</summary>
    Other
}