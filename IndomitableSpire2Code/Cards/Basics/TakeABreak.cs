using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Basics;

public sealed class TakeABreak() : IndomitableCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new HealVar(3M),
        new MotivationGainVar(15M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放台词、音频和技能动画
        await Owner.PlayIndomitableCardPresentation(cardPlay, Id.Entry, VfxColor.Gold,
            "res://IndomitableSpire2/sfx/characters/indomitable/get.wav",
            animationTrigger: "Cast", exactDurationSeconds: 10.5d);
        
        // 2. 回复生命值
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        
        // 3. 获得干劲
        await PowerCmd.Apply<MotivationPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars.MotivationGain().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
        
        // 4. 强制结束回合 (参考了 VoidForm 的写法)，第二个参数 false 代表这是不可撤销的操作
        PlayerCmd.EndTurn(Owner, false);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(1M);
        DynamicVars.MotivationGain().UpgradeValueBy(5M);
    }
}