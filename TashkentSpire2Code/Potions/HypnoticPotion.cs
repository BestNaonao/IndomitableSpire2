// using IndomitableSpire2.IndomitableSpire2Code.Powers;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using MegaCrit.Sts2.Core.Entities.Potions;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.HoverTips;
// using MegaCrit.Sts2.Core.Localization.DynamicVars;
//
// namespace TashkentSpire2.TashkentSpire2Code.Potions;
//
// public sealed class HypnoticPotion : TashkentPotion
// {
//     // 催眠是非常强力的控制，建议设置为罕见 (Uncommon) 或稀有 (Rare)
//     public override PotionRarity Rarity => PotionRarity.Uncommon;
//
//     public override PotionUsage Usage => PotionUsage.CombatOnly;
//
//     // 指定药水需要玩家手动选择一个敌人作为目标
//     public override TargetType TargetType => TargetType.AnyEnemy;
//
//     // 压缩图标和轮廓的资源路径
//     public override string PackedImagePath => 
//         "res://TashkentSpire2/images/potions/packed/hypnotic_potion.tres";
//     public override string PackedOutlinePath => 
//         "res://TashkentSpire2/images/potions/packed_outline/hypnotic_potion_outline.tres";
//     
//     // 定义药水数值：1 层催眠能力。STS2 中使用 PowerVar<T> 来绑定能力数值。
//     protected override IEnumerable<DynamicVar> CanonicalVars => 
//     [
//         new PowerVar<HypnotizedPower>(1M)
//     ];
//
//     // 添加悬浮提示 (HoverTip)：当玩家把鼠标放在药水上时，自动显示“催眠”这个能力的说明框
//     public override IEnumerable<IHoverTip> ExtraHoverTips => 
//     [
//         HoverTipFactory.FromPower<HypnotizedPower>()
//     ];
//
//     // 投掷药水时的实际逻辑
//     protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
//     {
//         // 安全性检查，确保目标不为空
//         ArgumentNullException.ThrowIfNull(target);
//
//         // 给选中的敌人 (target) 施加催眠能力
//         // 来源是玩家自身 (Owner.Creature)
//         await PowerCmd.Apply<HypnotizedPower>(
//             target: target, 
//             amount: DynamicVars["HypnotizedPower"].BaseValue, 
//             applier: Owner.Creature, 
//             cardSource: null
//         );
//     }
// }