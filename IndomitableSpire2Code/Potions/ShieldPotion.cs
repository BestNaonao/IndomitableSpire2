using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Potions;

public sealed class ShieldPotion : IndomitablePotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    
    public override TargetType TargetType => TargetType.AnyPlayer;
    
    public override string CustomPackedImagePath => 
        "res://IndomitableSpire2/images/potions/packed/shield_potion.tres";
    public override string CustomPackedOutlinePath => 
        "res://IndomitableSpire2/images/potions/packed_outline/shield_potion_outline.tres";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ShieldVar(10M, ValueProp.Unpowered)];
    
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShieldPower>()];
    
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        ArgumentNullException.ThrowIfNull(target);
        
        // 护盾需同时给予真实格挡及对应的跨回合保留能力。
        await CustomCreatureCmd.GainShield(
            choiceContext: choiceContext,
            target: target,
            blockVar: DynamicVars.Shield(),
            cardPlay: null,
            applier: Owner.Creature
        );
    }
}