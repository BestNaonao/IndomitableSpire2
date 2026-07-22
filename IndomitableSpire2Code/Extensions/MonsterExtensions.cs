using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class MonsterExtensions
{
    /// <summary>
    /// 尝试获取怪物当前的攻击意图（自动兼容单次、多段、致死打击）
    /// </summary>
    public static AttackIntent? GetAttackIntent(this MonsterModel monster)
    {
        // 【防御性编程】：如果怪物已经死亡或战斗状态丢失，它就没有意图伤害
        return !monster.Creature.IsAlive ? null :
            monster.NextMove.Intents.OfType<AttackIntent>().FirstOrDefault();
    }
    
    /// <summary>
    /// 获取怪物当前意图对指定目标玩家的单次攻击伤害。
    /// 原版 GetSingleDamage 内部硬编码使用 LocalContext.GetMe，会导致 Host 和 Client 
    /// 在计算非本地玩家受到的伤害时出现状态不同步。此重载允许显式传入目标实体，确保多端计算一致。
    /// </summary>
    public static int GetIntentSingleDamageTo(this MonsterModel monster, Player player)
    {
        var attackIntent = monster.GetAttackIntent();
        if (attackIntent?.DamageCalc == null) return 0;
        return Math.Max(0, (int) Hook.ModifyDamage(
            player.RunState, player.Creature.CombatState, player.Creature,
            monster.Creature, attackIntent.DamageCalc(), ValueProp.Move,
            null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _));
    }
    
    /// <summary>
    /// 获取怪物当前意图的攻击段数。
    /// 如果是单次攻击返回 1，多段攻击返回真实段数，不攻击返回 0。
    /// </summary>
    public static int GetIntentHitCount(this MonsterModel monster)
    {
        var attackIntent = monster.GetAttackIntent();
        return attackIntent?.Repeats ?? 0;
    }
    
    /// <summary>
    /// 获取怪物当前意图的总伤害（单次伤害 * 攻击段数）。
    /// </summary>
    public static int GetIntentTotalDamage(this MonsterModel monster)
    {
        var attackIntent = monster.GetAttackIntent();
        return attackIntent?.GetTotalDamage(monster.CombatState.PlayerCreatures, monster.Creature) ?? 0;
    }
    
    /// <summary>
    /// 判断怪物当前回合是否意图逃跑。
    /// </summary>
    public static bool IntendsToEscape(this MonsterModel monster) => monster.IntendsTo(IntentType.Escape);
    
    /// <summary>
    /// 判断怪物当前回合是否意图睡眠。
    /// </summary>
    public static bool IntendsToSleep(this MonsterModel monster) => monster.IntendsTo(IntentType.Sleep);
    
    /// <summary>
    /// 判断怪物当前回合是否包含某类意图。
    /// </summary>
    public static bool IntendsTo(this MonsterModel monster, IntentType type)
    {
        // 确保怪物存活且拥有下一步行动，然后检查其意图列表中是否包含目标意图类型
        return monster.Creature.IsAlive && 
               monster.NextMove.Intents.Any(intent => intent.IntentType == type);
    }
}