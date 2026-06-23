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
    
    // 封装判定逻辑，兼容普通升级牌与假升级的凋萎：不能是自己，并且是正常的已升级卡牌或被永世沙漏强化过的凋萎（只要伤害大于基础就说明被强化过）
    private bool CanBeDowngraded(CardModel c) => 
        c != this && (c.CurrentUpgradeLevel > 0 || c is Wither { DynamicVars.Damage.BaseValue: > 3M });
    
    // 核心限制：手牌中必须至少有一张已升级的牌（且不能是自己）才能打出
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(CanBeDowngraded);
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // ================= 阶段 1：选择降级 =================
        // 利用 FromHand 的第四个参数 (Predicate) 来过滤只能选已升级的牌
        var downPrefs = new CardSelectorPrefs(DowngradePrompt, 1, 2);
        var cardsToDowngrade = (await CardSelectCmd.FromHand(
            choiceContext, Owner, downPrefs, CanBeDowngraded, this)).ToList();
        
        if (cardsToDowngrade.Count == 0) return;
        
        var totalTransferLevels = 0;
        List<CardModel> cardsToPreview = [];
        
        // 遍历所有被选中的降级卡牌
        foreach (var cardToDowngrade in cardsToDowngrade)
        {
            // 特判剥夺“凋萎”的假升级层数
            if (cardToDowngrade is Wither witherTarget)
            {
                // 逆推层数：(当前伤害 - 基础伤害3) / 每层加的3
                totalTransferLevels += (int)((witherTarget.DynamicVars.Damage.BaseValue - 3M) / 3M);
                // 通过 Harmony 反射，强行将 Boss 赋予的私有假等级归零，并将基础伤害打回原形！
                var fakeUpgradeField = AccessTools.Field(typeof(Wither), "_fakeUpgradeLevel");
                if (fakeUpgradeField != null) fakeUpgradeField.SetValue(witherTarget, 0);
                witherTarget.DynamicVars.Damage.BaseValue = 3M;
            }
            else
            {
                // 正常牌走正常降级流程
                totalTransferLevels += cardToDowngrade.CurrentUpgradeLevel;
                CardCmd.Downgrade(cardToDowngrade);
            }
            cardsToPreview.Add(cardToDowngrade);
        }
        await Cmd.CustomScaledWait(0.2f, 0.4f);
        
        // ================= 阶段 2：选择升级 =================
        if (totalTransferLevels > 0)
        {
            // 【修改】：使用 CardPile.GetCards 合并获取手牌和抽牌堆的卡牌
            var validCardsForUpgrade = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw)
                .Where(c => !cardsToDowngrade.Contains(c) && c != this && c.IsUpgradable)
                .ToList();
            
            if (validCardsForUpgrade.Count > 0)
            {
                // 确保最大可选数量不超过有效卡牌的数量，防止底层越界
                var maxUpgrades = Math.Min(totalTransferLevels, validCardsForUpgrade.Count);
                var upPrefs = new CardSelectorPrefs(UpgradePrompt, 1, maxUpgrades);
                // 使用 FromSimpleGrid 呼出网格界面，彻底与手牌的物理布局解耦
                var cardsToUpgrade = await CardSelectCmd.FromSimpleGrid(
                    choiceContext, validCardsForUpgrade, Owner, upPrefs);
                
                foreach (var card in cardsToUpgrade)
                {
                    CardCmd.Upgrade(card);
                    cardsToPreview.Add(card);
                }
                await Cmd.CustomScaledWait(0.15f, 0.3f);
            }
        }
        // 统一在最后展示所有发生变化的卡牌（降级与升级的卡牌一起展示）
        if (cardsToPreview.Count > 0) CardCmd.Preview(cardsToPreview);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除虚无属性
        RemoveKeyword(CardKeyword.Ethereal);
    }
}