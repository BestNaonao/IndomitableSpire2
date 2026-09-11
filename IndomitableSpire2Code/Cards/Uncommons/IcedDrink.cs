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

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class IcedDrink() : IndomitableCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 注册变量：需求 10 点干劲，抽 2 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(10M),
        new CardsVar(2)
    ];
    
    // 悬浮提示框：干劲需求、起火、进水
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.Require),
        HoverTipFactory.FromPower<OnFirePower>(),
        HoverTipFactory.FromPower<FloodingPower>()
    ];
    
    // 核心限制：干劲需求
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        Owner.PlayIndomitableCardBanter(cardPlay, Id.Entry, VfxColor.Gold,
            "res://IndomitableSpire2/sfx/characters/indomitable/main_2_2.wav", exactDurationSeconds: 5.9d);
        var removedAmount = 0;
        
        // 1. 获取并移除敌人身上的所有“起火”
        if (cardPlay.Target.GetPower<OnFirePower>() is {} onFirePower)
        {
            removedAmount = onFirePower.Amount;
            await PowerCmd.Remove(onFirePower);
        }
        
        // 2. 如果成功移除了起火，给予等量的“进水”
        if (removedAmount > 0)
        {
            await PowerCmd.Apply<FloodingPower>(
                choiceContext: choiceContext,
                target: cardPlay.Target,
                amount: removedAmount,
                applier: Owner.Creature,
                cardSource: this
            );
        }
        
        // 3. 如果移除的数量大于等于 5，触发额外抽牌
        if (removedAmount >= 5) await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：耗能 -1
        EnergyCost.UpgradeBy(-1);
    }
}