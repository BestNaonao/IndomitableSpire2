using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class WoolworthReplenishment() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 添加能量相关的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip];
    
    // 注册变量：获得 1 点能量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 获得 1 点能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        
        // 2. 获取舰载机牌池
        var aircraftCards = ModelDb.AllCards.OfType<CarrierAircraftCard>();
        
        // 3. 随机抽取 1 张
        var card = CardFactory.GetDistinctForCombat(
            Owner, aircraftCards, 1, Owner.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();
        if (card == null) return;
        
        // 4. 【核心修改】：如果是升级版的“伍尔沃斯补给”，则生成的舰载机也被升级
        if (IsUpgraded) CardCmd.Upgrade(card);
        
        // 5. 将生成的卡牌加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
    
    // 升级效果已经通过 OnPlay 中的 IsUpgraded 和本地化文本的 IfUpgraded 标签实现了，这里不需要修改基础数值或费用，保持为空即可。
}