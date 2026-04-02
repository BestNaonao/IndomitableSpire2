using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Providers.DotProviders;

/// <summary>
/// 中毒持续伤害提供者
/// </summary>
public sealed class PoisonDotProvider : IDamageOverTimeProvider
{
    public string DamageTypeId => "PoisonForeground";
    public string DisplayName => "Poison";
    public int Priority => 10; // 最高优先级
    
    public Color ForegroundColor => new("76FF40");
    public Color LethalFontColor => new("76FF40");
    public Color LethalOutlineColor => new("074700");
    
    public int CalculateNextDamage(Creature creature)
    {
        var power = creature.GetPower<PoisonPower>();
        return power?.CalculateTotalDamageNextTurn() ?? 0;
    }
    
    public bool HasPower(Creature creature)
    {
        return creature.GetPower<PoisonPower>() is { };
    }
}