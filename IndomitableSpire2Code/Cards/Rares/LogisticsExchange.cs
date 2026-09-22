using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class LogisticsExchange() : IndomitableCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 利用 HarmonyLib 缓存私有字段的反射访问器，确保运行性能
    private static readonly FieldInfo LocalModifiersField = AccessTools.Field(typeof(CardEnergyCost), "_localModifiers");
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 呼出选择界面：强制要求选择2张手牌
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 2, 2);
        var selectedCards = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, 
            c => !c.EnergyCost.CostsX && !c.Keywords.Contains(CardKeyword.Unplayable), this
        )).ToList();
        
        if (selectedCards.Count == 2)
        {
            var cardA = selectedCards[0];
            var cardB = selectedCards[1];
            
            // 1. 获取两张卡牌当前显示在卡面上的最终能耗（包含了临时本地修饰符与全局光环）
            var currentCostA = cardA.EnergyCost.GetWithModifiers(CostModifiers.All);
            var currentCostB = cardB.EnergyCost.GetWithModifiers(CostModifiers.All);
            
            // 2. 将对方的能耗作为自己这场战斗的新基准
            ApplyCombatCost(cardA, currentCostB);
            ApplyCombatCost(cardB, currentCostA);
        }
    }
    
    /// <summary>
    /// 核心底层逻辑：无缝替换能耗基准
    /// </summary>
    private static void ApplyCombatCost(CardModel card, int targetCost)
    {
        // 获取底层的私有本地修饰符列表
        if (LocalModifiersField.GetValue(card.EnergyCost) is List<LocalCostModifier> localModifiers)
        {
            // 移除所有“本场战斗（EndOfCombat）”的永久修饰符
            // 防止在新的 BaseCost 上发生二次叠加，同时完美保留 ThisTurn 临时修饰符
            localModifiers.RemoveAll(m => m.Expiration == LocalCostModifierExpiration.EndOfCombat);
            
            // 致敬官方 UpgradeBy 机制，压制蛇眼的高费残留
            // 如果某张卡带有一个本回合绝对值修饰符（如蛇眼变成了 2 费），
            // 当它交换到了 0 费时，我们将这个修饰符的 Amount 强行钳制为 0！
            // 反之，如果它是 0 费增益（0 < targetCost），则不受影响，完美保留。
            foreach (var mod in localModifiers.Where(
                         mod => mod.Type == LocalCostType.Absolute && mod.Amount > targetCost)
                     ) mod.Amount = targetCost;
        }
        
        // 将目标能耗直接写入底层的 BaseCost
        // 这使得全局光环（如免费攻击）依然能在这个新 BaseCost 之上动态计算
        card.EnergyCost.SetCustomBaseCost(targetCost);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除消耗属性，使其可以在单局游戏中多次交换费用
        RemoveKeyword(CardKeyword.Exhaust);
    }
}