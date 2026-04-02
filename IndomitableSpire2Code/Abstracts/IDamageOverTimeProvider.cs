using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 持续伤害能力提供者接口
/// </summary>
public interface IDamageOverTimeProvider
{
    /// <summary>
    /// 伤害类型唯一标识（用于节点命名）
    /// </summary>
    string DamageTypeId { get; }
    
    /// <summary>
    /// 显示名称（用于日志）
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// 计算下一次行动将造成的伤害
    /// </summary>
    int CalculateNextDamage(Creature creature);
    
    /// <summary>
    /// 前景条颜色
    /// </summary>
    Color ForegroundColor { get; }
    
    /// <summary>
    /// 致命状态下的文字颜色
    /// </summary>
    Color LethalFontColor { get; }
    
    /// <summary>
    /// 致命状态下的文字描边颜色
    /// </summary>
    Color LethalOutlineColor { get; }
    
    /// <summary>
    /// 优先级（数字越小优先级越高，用于致死判断和显示顺序）
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// 检查该能力是否存在于生物身上
    /// </summary>
    bool HasPower(Creature creature);
}