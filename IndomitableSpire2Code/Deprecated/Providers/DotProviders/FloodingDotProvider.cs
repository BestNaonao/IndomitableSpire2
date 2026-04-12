// using Godot;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
// using IndomitableSpire2.IndomitableSpire2Code.Powers;
//
// namespace IndomitableSpire2.IndomitableSpire2Code.Providers.DotProviders;
//
// /// <summary>
// /// 进水持续伤害提供者
// /// </summary>
// public sealed class FloodingDotProvider : IDamageOverTimeProvider
// {
//     public string DamageTypeId => "FloodingForeground";
//     public string DisplayName => "Flooding";
//     public int Priority => 30; 
//     
//     // 天蓝色/水蓝色前景条，与能力图标和文本颜色呼应
//     public Color ForegroundColor => new("33CCFF");
//     // 致死时的文字颜色：亮青色 (明亮的荧光蓝)
//     public Color LethalFontColor => new("88FFFF");
//     // 致死时的文字描边：深海蓝 (保证在任何背景下都能看清文字)
//     public Color LethalOutlineColor => new("003366");
//     
//     public int CalculateNextDamage(Creature creature)
//     {
//         var power = creature.GetPower<FloodingPower>();
//         return power?.GetNextDamage() ?? 0; 
//     }
//     
//     public bool HasPower(Creature creature) => creature.GetPowerAmount<FloodingPower>() > 0;
// }