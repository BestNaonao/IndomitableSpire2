using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PlayerCombatStateTrackerExtensions
{
    // 利用 SpireField 无侵入地挂载到玩家的战斗状态上。生命周期与该场战斗完全绑定！
    public static readonly SpireField<PlayerCombatState, CombatPowerDamageTracker> PowerDamageTracker = 
        new(() => new CombatPowerDamageTracker());
    
    /// <summary>
    /// 记录某位玩家通过特定能力造成的伤害
    /// </summary>
    public static void RecordPowerDamage<TPower>(this PlayerCombatState combatState, int damageAmount)
    {
        PowerDamageTracker.Get(combatState)?.AddDamage(typeof(TPower), damageAmount);
    }
    
    /// <summary>
    /// 读取某位玩家在这场战斗中，通过特定能力造成的总伤害
    /// </summary>
    public static int GetTotalPowerDamage<TPower>(this PlayerCombatState combatState)
    {
        if (PowerDamageTracker.Get(combatState) is { } c)
            return c.GetDamage<TPower>();
        return 0;
    }
}