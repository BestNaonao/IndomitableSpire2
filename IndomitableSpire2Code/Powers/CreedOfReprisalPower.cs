using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class CreedOfReprisalPower : IndomitablePower
{
    // 参考 HellraiserPower 的固定保护值 9；这里按每次反击开始时的牌堆大小扩展上限。
    private const int MinimumNonAttackLimit = 9;
    
    private sealed class Data
    {
        public bool Resolving;
        public readonly Queue<(Creature Dealer, int Attacks)> Pending = new();
    }
    
    public override PowerType Type => PowerType.Buff;
    
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    protected override object InitInternalData() => new Data();
    
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 参考 火焰屏障 的反击时机及 狱火 的未格挡伤害判断。
        // 不检查 IsPoweredAttack：敌人造成的非攻击伤害也应触发；自伤、队友、无来源伤害不触发。
        if (target != Owner || result.UnblockedDamage <= 0 || dealer is not { IsEnemy: true } ||
            Owner.Player == null || Amount <= 0 || !CanContinue(dealer)) return;
        
        var data = GetInternalData<Data>();
        // 荆棘等效果可在本次自动打牌中再次伤害玩家。保留这些触发，在当前牌完成并消耗后依次结算，
        // 避免递归进入尚未完成的打牌流程。每次触发使用受伤时的层数，不被途中新增的能力追溯放大。
        data.Pending.Enqueue((dealer, Amount));
        if (data.Resolving) return;
        data.Resolving = true;
        try
        {
            while (data.Pending.TryDequeue(out var retaliation))
                if (CanContinue(retaliation.Dealer))
                    await Retaliate(choiceContext, retaliation.Dealer, retaliation.Attacks);
        }
        finally
        {
            data.Pending.Clear();
            data.Resolving = false;
        }
    }
    
    private bool CanContinue(Creature dealer) =>
        !CombatManager.Instance.IsOverOrEnding && Owner is { IsAlive: true, CombatState: { } combat } &&
        combat.ContainsCreature(dealer) && dealer.IsHittable;
    
    private async Task Retaliate(PlayerChoiceContext choiceContext, Creature dealer, int attacksRemaining)
    {
        var player = Owner.Player!;
        var drawPile = PileType.Draw.GetPile(player);
        var discardPile = PileType.Discard.GetPile(player);
        // 仅在循环开始时计算；途中生成新牌不会不断抬高上限。
        var nonAttackLimit = Math.Max(drawPile.Cards.Count + discardPile.Cards.Count, MinimumNonAttackLimit);
        var consecutiveNonAttacks = 0;
        Flash();
        
        // 参考 劫掠 的逐张处理并检查停止条件；不能预先取出多张，否则会改变洗牌和抽牌效果。
        while (attacksRemaining > 0 && consecutiveNonAttacks < nonAttackLimit && CanContinue(dealer))
        {
            // 参考 破灭 -> CardPileCmd.AutoPlayFromDrawPile：空堆先重洗，再取顶部第一张。
            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            if (!CanContinue(dealer) || drawPile.Cards.FirstOrDefault() is not { } card) break;
            
            await CardPileCmd.Add(card, PileType.Play);
            var isAttack = card.Type == CardType.Attack;
            // 由原版结算此次打牌的去向，保留重放、消耗钩子及动画；不永久给牌添加消耗关键词。
            // 非攻击牌维持自身原有去向，能力牌正常生效，已有消耗的技能牌仍然消耗。
            if (isAttack) card.ExhaustOnNextPlay = true;
            var history = CombatManager.Instance.History;
            var previousPlays = history.CardPlaysStarted.Count();
            // 破灭 的入口不接受指定目标，所以复用其内部指令。只给 AnyEnemy 传入伤害来源；
            // 自身、全体、友方目标牌继续使用原版 AutoPlay 的目标规则。
            await CardCmd.AutoPlay(choiceContext, card, card.TargetType == TargetType.AnyEnemy ? dealer : null);
            
            // AutoPlay 返回 Task 而非“是否打出”。用新增历史排除不能打出、被钩子禁止等情况。
            // 一张牌的原版重放仍是一轮取牌，不把它误当作从抽牌堆取出的多张攻击牌。
            if (isAttack && history.CardPlaysStarted.Skip(previousPlays).Any(entry => entry.CardPlay.Card == card))
            {
                attacksRemaining--;
                consecutiveNonAttacks = 0;
            }
            else
            {
                // 不能打出的牌同样推进无进展计数，避免诅咒/状态牌在抽弃牌堆间无限循环。
                consecutiveNonAttacks++;
            }
        }
    }
}