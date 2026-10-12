using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class DurabilityCmd
{
    /// <summary>结算耐久损失修正、数值变化、耗尽和后置钩子。DurableCard 的历史由 Internal 方法记录。</summary>
    public static async Task LoseDurability(PlayerChoiceContext choiceContext, CardModel card, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (!card.HasDurability()) return;
        card.AssertMutable();
        var durability = card.DynamicVars.Durability();
        var loss = CustomHook.ModifyDurabilityLossInCombat(card.CombatState, card, amount, out var models);
        if (loss > 0)
        {
            if (card is DurableCard durable) durable.LoseDurabilityInternal(loss);
            else durability.BaseValue = Math.Max(0, durability.BaseValue - loss);
            if (durability.BaseValue <= 0 && card.CombatState != null)
                await CardCmd.Exhaust(choiceContext, card);
        }
        await CustomHook.AfterModifyingDurabilityLossInCombat(card.CombatState, card, models);
    }
    
    /// <summary>恢复当前耐久；卡牌必须同时具有当前耐久和最大耐久变量。</summary>
    /// <returns>实际恢复量。</returns>
    public static async Task<int> GainDurability(CardModel card, int amount, bool skipVisuals = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0 || !card.HasDurability()) return 0;
        card.AssertMutable();
        var durability = card.DynamicVars.Durability();
        var maximum = card.DynamicVars.MaxDurability();
        var heal = Math.Min(amount, Math.Max(0, maximum.IntValue - durability.IntValue));
        if (heal == 0) return 0;
        // 先结算实际数值和 DurableCard 的变化记录，再展示结果。
        // NCard 刷新会清除临时 PreviewValue，维修不能依赖手动设置的预测恢复量。
        if (card is DurableCard durable) heal = durable.GainDurabilityInternal(heal);
        else
        {
            var previous = durability.BaseValue;
            durability.BaseValue = Math.Min(previous + heal, maximum.BaseValue);
            heal = (int)(durability.BaseValue - previous);
        }
        if (skipVisuals || heal <= 0) return heal;
        CardCmd.Preview(card);
        await Cmd.CustomScaledWait(0.2f, 0.5f);
        return heal;
    }
    
    /// <summary>增加最大耐久，然后像 GainMaxHp 一样恢复等量当前耐久。</summary>
    public static async Task GainMaxDurability(CardModel card, int amount, bool skipVisuals = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0 || !card.HasDurability()) return;
        card.AssertMutable();
        var maximum = card.DynamicVars.MaxDurability();
        int gained;
        if (card is DurableCard durable) gained = durable.GainMaxDurabilityInternal(amount);
        else
        {
            gained = Math.Min(amount, DurableCard.DurabilityLimit - maximum.IntValue);
            maximum.BaseValue += gained;
        }
        await GainDurability(card, gained, skipVisuals);
    }
    
    /// <summary>降低最大耐久，将当前耐久限制到新上限；卡牌必须同时具有两个耐久变量。</summary>
    public static async Task LoseMaxDurability(PlayerChoiceContext choiceContext, CardModel card, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0 || !card.HasDurability()) return;
        card.AssertMutable();
        var maximum = card.DynamicVars.MaxDurability();
        var durability = card.DynamicVars.Durability();
        if (card is DurableCard durable) durable.LoseMaxDurabilityInternal(amount);
        else
        {
            maximum.BaseValue = Math.Max(0, maximum.BaseValue - amount);
            durability.BaseValue = Math.Min(durability.BaseValue, maximum.BaseValue);
        }
        // 最大值降低造成的裁剪不是一次受击，不再经过耐久损失修正。
        if (durability.BaseValue <= 0 && card.CombatState != null)
            await CardCmd.Exhaust(choiceContext, card);
    }
    
    public static async Task FullyRepair(CardModel card, bool skipVisuals = false)
    {
        if (card.IsFullDurability()) return;
        await GainDurability(card,
            card.DynamicVars.MaxDurability().IntValue - card.DynamicVars.Durability().IntValue, skipVisuals);
    }
    
    /// <summary>在调用方提供的候选牌中，随机分配指定的总恢复量。</summary>
    public static async Task RandomRepair(Player player, IEnumerable<CardModel> candidateCards, int totalHeal, bool skipVisuals = false)
    {
        var candidates = candidateCards.Where(card => card.HasDurability()).ToList();
        while (totalHeal > 0)
        {
            var targets = candidates.Where(card => !card.IsFullDurability()).ToList();
            if (targets.Count == 0) return;
            var target = player.RunState.Rng.CombatCardSelection.NextItem(targets)!;
            var healed = await GainDurability(target, totalHeal, skipVisuals);
            if (healed == 0) candidates.Remove(target);
            totalHeal -= healed;
        }
    }
}