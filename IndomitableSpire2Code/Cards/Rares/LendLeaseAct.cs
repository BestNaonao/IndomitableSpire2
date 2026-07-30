using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class LendLeaseAct() : IndomitableCard(2, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
{
    // 仅限多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    private static bool Filter(CardModel c) => c.Type is CardType.Attack or CardType.Skill or CardType.Power;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 获取目标玩家（另一名玩家）
        var otherPlayer = cardPlay.Target.Player;
        if (otherPlayer is not { Creature.IsAlive: true }) return;
        
        var myPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var otherPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        
        // 2. 发起选牌指令。FromHand 内部如果发现传入的是 BlockingPlayerChoiceContext，它会通过底层网络同步器去接管全局死锁。
        var myTask = CardSelectCmd.FromHand(
            new BlockingPlayerChoiceContext(), Owner, myPrefs, Filter, this);
        var otherTask = CardSelectCmd.FromHand(
            new BlockingPlayerChoiceContext(), otherPlayer, otherPrefs, Filter, this);
        
        // 3. 并发等待双方选择完毕
        await Task.WhenAll(myTask, otherTask);
        
        // 4. 双方都选完后，安全地提取结果
        var myCard = myTask.Result.FirstOrDefault();
        var otherCard = otherTask.Result.FirstOrDefault();
        
        // 5. 执行契约并施加追溯能力
        if (myCard != null && otherCard != null)
        {
            // 赋予本回合免费打出
            myCard.SetToFreeThisTurn();
            otherCard.SetToFreeThisTurn();
            
            // 部署双向四份追溯契约，处理我选的牌 (myCard)和对方选的牌 (otherCard)
            await ApplyCardContracts(choiceContext, Owner, myCard, otherPlayer, true);
            await ApplyCardContracts(choiceContext, otherPlayer, myCard, Owner, false);
            await ApplyCardContracts(choiceContext, otherPlayer, otherCard, Owner, true);
            await ApplyCardContracts(choiceContext, Owner, otherCard, otherPlayer, false);
        }
    }
    
    private async Task ApplyCardContracts(
        PlayerChoiceContext choiceContext, Player from, CardModel card, Player to, bool isLending)
    {
        var power = await PowerCmd.Apply<LendLeaseActPower>(
            choiceContext, from.Creature, 1M, Owner.Creature, this);
        power?.SetContract(card, to, isLending);
    }
    
    protected override void OnUpgrade()
    {
        // 升级后不再消耗
        RemoveKeyword(CardKeyword.Exhaust);
    }
}