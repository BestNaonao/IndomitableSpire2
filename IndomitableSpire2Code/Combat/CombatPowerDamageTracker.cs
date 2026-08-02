namespace IndomitableSpire2.IndomitableSpire2Code.Combat;

/// <summary>
/// 战斗内能力伤害追踪器。用于统计玩家在单场战斗中，通过各种能力（Power）造成的总伤害。
/// </summary>
public class CombatPowerDamageTracker
{
    // 按能力类型分类记录累计伤害
    private readonly Dictionary<Type, int> _damageByPowerType = new();
    
    public void AddDamage(Type powerType, int damage)
    {
        if (damage <= 0) return;
        if (!_damageByPowerType.TryAdd(powerType, damage))
        {
            _damageByPowerType[powerType] += damage;
        }
    }
    
    public int GetDamage<TPower>() => GetDamage(typeof(TPower));
    public int GetDamage(Type powerType) => _damageByPowerType.GetValueOrDefault(powerType, 0);
}