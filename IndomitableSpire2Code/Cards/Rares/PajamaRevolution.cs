using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class PajamaRevolution() : IndomitableCard(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 包含“消耗”关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 注册变量：2层催眠，8点格挡，1点能量
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new BlockVar(8M, ValueProp.Move),
        new CustomPowerVar<HypnotizedPower>(2M),
        new EnergyVar(1)
    ];
    
    // 提供悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<HypnotizedPower>(), EnergyHoverTip];
    
    // 必须声明 GainsBlock 才能让系统正确计算护甲相关的 Hook
    public override bool GainsBlock => true;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 给予目标敌人催眠
        if (cardPlay.Target is { IsAlive: true })
        {
            await PowerCmd.Apply<HypnotizedPower>(
                choiceContext: choiceContext, 
                target: cardPlay.Target,
                amount: DynamicVars.Hypnotized().BaseValue,
                applier: Owner.Creature,
                cardSource: this
            );
        }
        
        // 2. 使用 CombatState.PlayerCreatures 获取所有存活的玩家生物实体
        if (CombatState != null)
        {
            foreach (var playerCreature in CombatState.PlayerCreatures.Where(c => c.IsAlive))
            {
                // 所有玩家获得格挡，并在下回合获得能量
                await CreatureCmd.GainBlock(playerCreature, DynamicVars.Block, cardPlay);
                await PowerCmd.Apply<EnergyNextTurnPower>(
                    choiceContext: choiceContext, 
                    target: playerCreature,
                    amount: DynamicVars.Energy.BaseValue,
                    applier: Owner.Creature,
                    cardSource: this
                );
            }
            
            // 3. 强制结束所有玩家的回合：既然大家都要睡觉了，那就一起结束吧
            foreach (var player in CombatState.Players)
            {
                // false 表示这不是因为回合自动超时结束的，而是因为卡牌效果强制结束的
                PlayerCmd.EndTurn(player, false);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：全队格挡 +2 (变为10点)，能量消耗 -1 (变为2点)
        DynamicVars.Block.UpgradeValueBy(2M);
        EnergyCost.UpgradeBy(-1);
    }
}