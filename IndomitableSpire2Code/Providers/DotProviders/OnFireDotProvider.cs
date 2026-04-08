// using Godot;
// using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
// using IndomitableSpire2.IndomitableSpire2Code.Powers;
// using MegaCrit.Sts2.Core.Entities.Creatures;
//
// namespace IndomitableSpire2.IndomitableSpire2Code.Providers.DotProviders;
//
// /// <summary>
// /// 起火持续伤害提供者
// /// </summary>
// public sealed class OnFireDotProvider : IDamageOverTimeProvider
// {
//     public string DamageTypeId => "OnFireForeground";
//     public string DisplayName => "OnFire";
//     public int Priority => 20; // 中毒(10) < 起火(20) < 灾厄(30)
//     
//     public Color ForegroundColor => new("FFA200");
//     public Color LethalFontColor => new("FFD700");
//     public Color LethalOutlineColor => new("4A3C00");
//     
//     public int CalculateNextDamage(Creature creature)
//     {
//         var power = creature.GetPower<OnFirePower>();
//         return power?.GetNextDamage() ?? 0;
//     }
//     
//     public bool HasPower(Creature creature) => creature.GetPowerAmount<OnFirePower>() > 0;
// }