using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class BurningPassion() : IndomitableCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    // 注册变量：需求 20 点干劲，获得 2 点能量
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(20M),
        new EnergyVar(2)
    ];
    
    // 悬浮提示框：干劲需求、进水、起火、能量图标说明
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.Require),
        HoverTipFactory.FromPower<FloodingPower>(),
        HoverTipFactory.FromPower<OnFirePower>(),
        HoverTipFactory.ForEnergy(this)
    ];
    
    // 核心限制：干劲需求
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await Owner.PlayIndomitableCardPresentation(cardPlay, Id.Entry, VfxColor.Gold,
            "res://IndomitableSpire2/sfx/characters/indomitable/main_3_2.wav",
            animationTrigger: "Cast", exactDurationSeconds: 9.4d);
        var removedAmount = 0;
        
        // 1. 获取并移除敌人身上的所有“进水”
        if (cardPlay.Target.GetPower<FloodingPower>() is {} floodingPower)
        {
            removedAmount = floodingPower.Amount;
            await PowerCmd.Remove(floodingPower);
        }
        
        // 2. 如果成功移除了进水，给予等量的“起火”
        if (removedAmount > 0)
        {
            await PowerCmd.Apply<OnFirePower>(
                choiceContext: choiceContext,
                target: cardPlay.Target,
                amount: removedAmount,
                applier: Owner.Creature,
                cardSource: this
            );
        }
        
        // 3. 如果移除的数量大于等于 1，触发额外回费
        if (removedAmount >= 1) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：额外能量 +1（变为3）
        DynamicVars.Energy.UpgradeValueBy(1M);
    }
}