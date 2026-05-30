using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class PilotTransfer() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    // 自定义选择界面的提示文本
    private static LocString DowngradePrompt => new("card_selection", "INDOMITABLESPIRE2-TRANSFER_DOWNGRADE");
    private static LocString UpgradePrompt => new("card_selection", "INDOMITABLESPIRE2-TRANSFER_UPGRADE");
    
    // 核心限制：手牌中必须至少有一张已升级的牌（且不能是自己）才能打出
    protected override bool IsPlayable => 
        PileType.Hand.GetPile(Owner).Cards.Any(c => c.CurrentUpgradeLevel > 0 && c != this);
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // ================= 阶段 1：选择降级 =================
        var downPrefs = new CardSelectorPrefs(DowngradePrompt, 1);
        
        // 利用 FromHand 的第四个参数 (Predicate) 来过滤只能选已升级的牌
        var cardToDowngrade = (await CardSelectCmd.FromHand(
            choiceContext, Owner, downPrefs, c => c.CurrentUpgradeLevel > 0 && c != this, this
        )).FirstOrDefault();
        
        if (cardToDowngrade == null) return;
        
        // 记录它身上的所有“王牌经验”
        var transferLevels = cardToDowngrade.CurrentUpgradeLevel;
        
        // 执行彻底降级，并在屏幕上闪烁展示，给予负向但清晰的视觉反馈
        CardCmd.Downgrade(cardToDowngrade);
        CardCmd.Preview(cardToDowngrade);
        
        // 稍微停顿一下，让玩家看清降级发生了
        await Cmd.CustomScaledWait(0.3f, 0.5f);
        
        // ================= 阶段 2：选择升级 =================
        if (transferLevels > 0)
        {
            // 提示文本中可以使用动态变量 {Amount} 来显示能升几级
            var upPrefs = new CardSelectorPrefs(UpgradePrompt, 1, transferLevels);
            
            // 过滤：只能选除了刚才被降级的那张牌之外的牌（防止刚降级又给升回去，破坏风味）
            var cardsToUpgrade = await CardSelectCmd.FromHand(
                choiceContext, Owner, upPrefs, c => c != cardToDowngrade && c != this && c.IsUpgradable, this
                );
            
            foreach (var card in cardsToUpgrade)
            {
                CardCmd.Upgrade(card);
                CardCmd.Preview(card); // 闪烁展示升级效果
                await Cmd.CustomScaledWait(0.15f, 0.3f);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：费用 -1（变为 0 费，润滑流转节奏）
        EnergyCost.UpgradeBy(-1);
    }
}