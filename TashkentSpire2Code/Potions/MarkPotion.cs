using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Potions;

public sealed class MarkPotion : TashkentPotion
{
     public override PotionRarity Rarity => PotionRarity.Common;

     public override PotionUsage Usage => PotionUsage.CombatOnly;
     
     public override TargetType TargetType => TargetType.AnyEnemy;
     
     protected override IEnumerable<DynamicVar> CanonicalVars => 
     [
         new PowerVar<MarkPower>(6M)
     ];
     
     public override IEnumerable<IHoverTip> ExtraHoverTips => 
     [
         HoverTipFactory.FromPower<MarkPower>()
     ];
     
     protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
     {
         ArgumentNullException.ThrowIfNull(target);
         
         await PowerCmd.Apply<MarkPower>(
             target: target, 
             amount: DynamicVars["MarkPower"].BaseValue, 
             applier: Owner.Creature, 
             cardSource: null
         );
     }
}