using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class DualPurposeGun() : IndomitableCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 必须重写此属性，以便游戏系统能自动在卡牌旁边展示“格挡：防止受到生命损伤”的悬浮提示框
    public override bool GainsBlock => true;
    
    // 提取复用逻辑：这张牌这回合是否被打出过？
    private bool HasBeenPlayedThisTurn => CombatState != null && CombatManager.Instance.History.CardPlaysFinished
        .Any(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card == this);
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7M, ValueProp.Move),
        new BlockVar(8M, ValueProp.Move),
        // 自定义变量：查询战斗历史，判断这张牌本回合是否被打出过，用于 UI 动态颜色提示
        new CalculationBaseVar(0M),             // 使用 CalculatedVar 所必要的
        new CalculationExtraVar(1M),    // 使用 CalculatedVar 所必要的
        new CalculatedVar("WillAutoBlock").WithMultiplier((card, _) => 
            ((DualPurposeGun)card).HasBeenPlayedThisTurn ? 0M : 1M)
    ];
    
    // ========== 核心机制：统一在 OnPlay 中处理效果 ==========
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 区分是自动打出（防空格挡）还是手动打出（平射伤害）
        if (cardPlay.IsAutoPlay)
        {
            // 【自动打出】：高射防空。这样就能完美吃满敏捷加成，且计算为一次正常的卡牌打出！
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
        else
        {
            // 【手动打出】：平射对舰
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
    
    // ========== 触发机制：回合结束的判定与自动打出 ==========
    public override bool HasTurnEndInHandEffect => !HasBeenPlayedThisTurn;
    
    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 既然能进到这里，说明 HasTurnEndInHandEffect 必定为 true，即本回合未被打出。
        // 触发自动打出：高射防空（留在手中自动提供格挡）
        await Cmd.Wait(0.25f); // 停顿一下，给予玩家防空机制触发的视觉反馈节奏
        await CardCmd.AutoPlay(choiceContext, this, null);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：伤害 +3，格挡 +3
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Block.UpgradeValueBy(2M);
    }
}