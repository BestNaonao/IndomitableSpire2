using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class LaunchCeremony() : CommissionCard(TargetType.Self)
{
    // 初始需要打出 16 张牌
    protected override int InitialMaxProgressAmount => 16;
    
    // 监听任意卡牌打出
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 确保是持卡方自己打出的牌
        if (cardPlay.Player == Owner) AddProgress(1);
        return Task.CompletedTask;
    }
    
    protected override Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        // 效仿原版“狩猎(The Hunt)”，将卡牌奖励发放到战斗结束的奖励界面
        if (Owner.RunState.CurrentRoom is CombatRoom combatRoom)
        {
            // 额外发放一个标准的 3 选 1 卡牌奖励（基于当前房间的类型）
            combatRoom.AddExtraReward(player, new CardReward(CardCreationOptions.ForRoom(player, combatRoom.RoomType), 3, player));
        }
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：需要的打牌数量减少 4 张（变为 12 张）
        DynamicVars["MaxProgress"].UpgradeValueBy(-4M);
    }
}