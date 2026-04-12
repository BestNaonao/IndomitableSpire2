using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

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
    /// 获取怪物当前意图的单次攻击伤害（已计算力量、虚弱、易伤等最终面板伤害）
    /// 如果怪物当前不打算攻击，返回 0。
    /// </summary>
    public static int GetIntentSingleDamage(this MonsterModel monster)
    {
        var attackIntent = monster.GetAttackIntent();
        return attackIntent?.GetSingleDamage(monster.CombatState.PlayerCreatures, monster.Creature) ?? 0;
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
}