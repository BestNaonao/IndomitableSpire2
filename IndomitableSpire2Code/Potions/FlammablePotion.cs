using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Potions;

[Pool(typeof(SharedPotionPool))]
public sealed class FlammablePotion : IndomitableSpire2Potion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    
    public override TargetType TargetType => TargetType.AnyEnemy;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CustomPowerVar<OnFirePower>(5M)];
    
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<OnFirePower>()];
    
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        ArgumentNullException.ThrowIfNull(target);
        
        await PowerCmd.Apply<OnFirePower>(
            choiceContext: choiceContext,
            target: target,
            amount: DynamicVars.OnFire().BaseValue,
            applier: Owner.Creature,
            cardSource: null
        );
    }
}