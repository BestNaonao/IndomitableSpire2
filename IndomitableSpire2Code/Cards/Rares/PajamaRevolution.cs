using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class PajamaRevolution() : IndomitableCard(3, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    // 包含“消耗”关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 注册变量：用两个不同名字的变量分别管理对敌和对友的催眠层数
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new PowerVar<HypnotizedPower>("EnemyHypnotized", 5M),
        new PowerVar<HypnotizedPower>("AllyHypnotized", 5M),
        new EnergyVar(1)
    ];
    
    // 提供悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<HypnotizedPower>(), EnergyHoverTip];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放台词、语音和动画，稍后执行逻辑
        await Owner.PlayIndomitableCardPresentation(cardPlay, Id.Entry, VfxColor.Gold, 
            "res://IndomitableSpire2/sfx/characters/indomitable/feeling5_2.wav", 
            animationTrigger: "Cast", exactDurationSeconds: 7.5d);
        await Cmd.CustomScaledWait(0.2f, 0.4f);
        if (CombatState != null)
        {
            foreach (var creature in CombatState.Creatures.Where(c => c.IsAlive))
            {
                // 给予催眠
                await PowerCmd.Apply<HypnotizedPower>(
                    choiceContext: choiceContext,
                    target: creature,
                    amount: creature.IsEnemy ? 
                        DynamicVars["EnemyHypnotized"].BaseValue : DynamicVars["AllyHypnotized"].BaseValue,
                    applier: Owner.Creature,
                    cardSource: this);
                // 给予所有玩家下回合的能量
                if (creature.IsPlayer)
                {
                    await PowerCmd.Apply<EnergyNextTurnPower>(
                        choiceContext: choiceContext,
                        target: creature,
                        amount: DynamicVars.Energy.BaseValue,
                        applier: Owner.Creature,
                        cardSource: this);
                }
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：仅增加给敌人的层数，并增加下回合的能量。给盟友的层数不变。
        DynamicVars["EnemyHypnotized"].UpgradeValueBy(3M);
        DynamicVars.Energy.UpgradeValueBy(1M);
    }
}