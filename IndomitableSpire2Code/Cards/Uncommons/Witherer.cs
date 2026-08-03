using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons; // 假设为罕见/稀有

public sealed class Witherer() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 单人模式基础伤害门槛
    public const decimal InitialThreshold = 20M;
    
    // 用于统计造成的 DOT 伤害和多人模式下缩放的伤害门槛的静态方法
    public static int DealtDotDamage(Player? player) =>
        (player?.PlayerCombatState?.GetTotalPowerDamage<OnFirePower>() ?? 0) + 
        (player?.PlayerCombatState?.GetTotalPowerDamage<FloodingPower>() ?? 0);
    
    public static int ScaledThreshold(ICombatState? combatState) => 
        (int)(InitialThreshold * (combatState?.Players.Count ?? 1));
    
    // 注册：1层能力，伤害阈值49
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new PowerVar<WithererPower>(1M),
        new("Threshold", ScaledThreshold(CombatState))
    ];
    
    // 卡牌高亮：当造成的 DOT 总伤害达到门槛时，手牌中的卡牌闪烁金光
    protected override bool ShouldGlowGoldInternal => DealtDotDamage(Owner) >= ScaledThreshold(CombatState);
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        await PowerCmd.Apply<WithererPower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars["WithererPower"].BaseValue, 
            Owner.Creature, 
            this
        );
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["WithererPower"].UpgradeValueBy(1M);
    }
}