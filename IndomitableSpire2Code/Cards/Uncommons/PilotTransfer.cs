using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class PilotTransfer() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    // 自定义选择界面的提示文本
    private static LocString DowngradePrompt => new("card_selection", "INDOMITABLESPIRE2-TRANSFER_DOWNGRADE");
    private static LocString UpgradePrompt => new("card_selection", "INDOMITABLESPIRE2-TRANSFER_UPGRADE");
    
    // 【核心修复 1】：封装判定逻辑，兼容普通升级牌与假升级的凋萎
    // 不能是自己，并且是正常的已升级卡牌或被永世沙漏强化过的凋萎（只要伤害大于基础的 3，就说明被强化过）
    private bool CanBeDowngraded(CardModel c) => 
        c != this && (c.CurrentUpgradeLevel > 0 || c is Wither { DynamicVars.Damage.BaseValue: > 3M });
    
    // 核心限制：手牌中必须至少有一张已升级的牌（且不能是自己）才能打出
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(CanBeDowngraded);
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // ================= 阶段 1：选择降级 =================
        var downPrefs = new CardSelectorPrefs(DowngradePrompt, 1);
        
        // 利用 FromHand 的第四个参数 (Predicate) 来过滤只能选已升级的牌
        var cardToDowngrade = (await CardSelectCmd.FromHand(
            choiceContext, Owner, downPrefs, CanBeDowngraded, this)).FirstOrDefault();
        
        if (cardToDowngrade == null) return;
        
        int transferLevels;
        List<CardModel> cardsToPreview = [cardToDowngrade];
        // 【核心修复 2】：特判剥夺“凋萎”的假升级层数
        // ==========================================
        if (cardToDowngrade is Wither witherTarget)
        {
            // 逆推层数：(当前伤害 - 基础伤害3) / 每层加的3
            transferLevels = (int)((witherTarget.DynamicVars.Damage.BaseValue - 3M) / 3M);
            // 通过 Harmony 反射，强行将 Boss 赋予的私有假等级归零，并将基础伤害打回原形！
            var fakeUpgradeField = AccessTools.Field(typeof(Wither), "_fakeUpgradeLevel");
            if (fakeUpgradeField != null) fakeUpgradeField.SetValue(witherTarget, 0);
            witherTarget.DynamicVars.Damage.BaseValue = 3M;
        }
        else
        {
            // 正常牌走正常降级流程
            transferLevels = cardToDowngrade.CurrentUpgradeLevel;
            CardCmd.Downgrade(cardToDowngrade);
        }
        await Cmd.CustomScaledWait(0.2f, 0.4f);
        
        // ================= 阶段 2：选择升级 =================
        if (transferLevels > 0)
        {
            // 提示文本中可以使用动态变量 {Amount} 来显示能升几级
            var upPrefs = new CardSelectorPrefs(UpgradePrompt, 1, transferLevels);
            
            // 手动过滤出符合条件的卡牌列表
            var validCardsForUpgrade = PileType.Hand.GetPile(Owner).Cards
                .Where(c => c != cardToDowngrade && c != this && c.IsUpgradable)
                .ToList();
            
            if (validCardsForUpgrade.Count > 0)
            {
                // 【核心修复 2】：使用 FromSimpleGrid 呼出网格界面，彻底与手牌的物理布局解耦
                var cardsToUpgrade = await CardSelectCmd.FromSimpleGrid(
                    choiceContext, validCardsForUpgrade, Owner, upPrefs
                );
                
                foreach (var card in cardsToUpgrade)
                {
                    CardCmd.Upgrade(card);
                    cardsToPreview.Add(card);
                    await Cmd.CustomScaledWait(0.15f, 0.3f);
                }
            }
        }
        // 展示降级和升级的卡牌
        CardCmd.Preview(cardsToPreview);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除虚无属性
        RemoveKeyword(CardKeyword.Ethereal);
    }
}