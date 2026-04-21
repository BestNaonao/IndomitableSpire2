using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Tokens;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class DispatchCommission() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    // 仅限多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 1. 随机抽取一种委托牌类型
        // 【核心修复】：使用 CombatState.CreateCard，赋予卡牌合法的战斗生命周期
        CommissionCard generatedCard = Owner.RunState.Rng.CombatCardGeneration.NextInt(0, 4) switch
        {
            0 => CombatState!.CreateCard<OilCommission>(cardPlay.Target.Player!), // 注意：要把目标玩家传进去作为所有者
            1 => CombatState!.CreateCard<ExerciseCommission>(cardPlay.Target.Player!),
            2 => CombatState!.CreateCard<TransportCommission>(cardPlay.Target.Player!),
            _ => CombatState!.CreateCard<EscortCommission>(cardPlay.Target.Player!)
        };

        // 2. 【核心逻辑】将打出母牌的玩家设为这单委托的“委托方（甲方）”
        generatedCard.Delegator = Owner;

        // 3. 继承升级状态
        if (IsUpgraded)
            CardCmd.Upgrade(generatedCard);

        // 4. 发送到队友手牌中
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, true);
    }

    protected override void OnUpgrade()
    {
        // 升级将费用降为 0
        EnergyCost.UpgradeBy(-1);
    }
}