using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Wither = MegaCrit.Sts2.Core.Models.Cards.Wither;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class OnePersonSchool() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private static readonly AccessTools.FieldRef<Wither, int> WitherUpgradeLevel =
        AccessTools.FieldRefAccess<Wither, int>("_fakeUpgradeLevel");
    
    protected override bool IsPlayable => Owner.PlayerCombatState?.Hand.Cards
        .Any(card => card != this && card.IsTransformable) == true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Insight>(IsUpgraded)];
    
    private void UpgradeInsight(Insight insight, CardModel original)
    {
        // 心得等级 = 一人学派等级 + 所选牌等级；用 long 求和避免其它 MOD 的高等级溢出。
        var upgrades = (long)CurrentUpgradeLevel + original.CurrentUpgradeLevel;
        // 凋萎的 FakeUpgrade 不改变 CurrentUpgradeLevel，需要计入其单独记录的升级次数。
        // 读取实际次数，避免把其它效果增加的伤害误算为升级。
        if (original is Wither wither)
            upgrades += Math.Max(0, WitherUpgradeLevel(wither));
        upgrades = Math.Min(upgrades, insight.MaxUpgradeLevel);
        while (insight.CurrentUpgradeLevel < upgrades)
            insight.UpgradeInternal();
        insight.FinalizeUpgradeInternal();
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner, prefs, card => card.IsTransformable, this);
        
        foreach (var original in selected)
        {
            var insight = CombatState.CreateCard<Insight>(Owner);
            UpgradeInsight(insight, original);
            // 变化保留原牌的附魔及层数，包括通常只能附在攻击牌上的附魔。
            // 复制独立实例，避免心得和原牌共享附魔的所属卡牌及可变状态。
            if (original.Enchantment is { } enchantment)
            {
                var copy = (EnchantmentModel)enchantment.ClonePreservingMutability();
                insight.EnchantInternal(copy, copy.Amount);
                copy.ModifyCard();
                insight.FinalizeUpgradeInternal();
            }
            await CardCmd.Transform(original, insight);
        }
    }
    
    // 本牌的升级次数由 UpgradeInsight 叠加到所选牌的升级次数上。
    protected override void OnUpgrade() { }
}