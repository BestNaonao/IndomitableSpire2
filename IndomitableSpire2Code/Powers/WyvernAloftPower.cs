using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class WyvernAloftPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    
    protected override object InitInternalData() => new Data();
    
    private sealed class Data
    {
        public WyvernS4? Card;
        public bool TriggeredThisTurn;
        public bool Resolving;
    }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => GetInternalData<Data>().Card is { } card
        ? [HoverTipFactory.FromCard(new WyvernFighter().CreatePreview(card))] : [];
    
    public static async Task ApplyFor(PlayerChoiceContext context, WyvernS4 card)
    {
        // 有任何指向这张卡的“飞龙在天”能力就不重复给予。
        if (card.Owner.Creature.Powers.OfType<WyvernAloftPower>().Any(power => power.GetInternalData<Data>().Card == card)) return;
        await PowerCmd.Apply<WyvernAloftPower>(context, card.Owner.Creature, 1, card.Owner.Creature, card);
    }
    
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        data.Card = cardSource as WyvernS4;
        // 也防护外部通过 PowerCmd.Apply 直接施加同卡实例；旧实例的本回合次数不重置。
        if (data.Card == null || Owner.Powers.OfType<WyvernAloftPower>()
                .Any(power => power != this && power.GetInternalData<Data>().Card == data.Card))
            await PowerCmd.Remove(this);
    }
    
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // 参考原版 Skittish 的“本回合已触发”标记；每个阵营回合开始重新允许一次。
        GetInternalData<Data>().TriggeredThisTurn = false;
        return Task.CompletedTask;
    }
    
    public override async Task AfterAttack(PlayerChoiceContext context, AttackCommand command)
    {
        var data = GetInternalData<Data>();
        if (data.TriggeredThisTurn || data.Resolving || 
            command.Attacker is not { IsEnemy: true } attacker || !command.DamageProps.IsPoweredAttack() || 
            command.Results.SelectMany(hit => hit).All(result => result.Receiver != Owner)) return;
        // 先消耗首次攻击的机会，再 await，防止多段/嵌套攻击重入；完全格挡仍属于被攻击。
        data.TriggeredThisTurn = true;
        if (Owner.IsDead || !attacker.IsHittable || data.Card is not { } card ||
            card.Owner.Creature != Owner || card.CombatState != CombatState ||
            card.Pile?.Type is not (PileType.Draw or PileType.Discard)) return;
        data.Resolving = true;
        try
        {
            Flash();
            await card.AutoPlayAloft(context, attacker);
        }
        finally { data.Resolving = false; }
    }
    
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != GetInternalData<Data>().Card) return;
        // 消耗代表被击坠，立即结束本次绑定；之后打捞、修复或换回其他牌堆都不会恢复旧能力。
        // 回到手牌、移出战斗或转交也结束绑定。再次打出鱼雷挂载仍可正常重新获得绑定。
        if (card.Pile == null || card.Owner.Creature != Owner|| 
            card.Pile.Type is PileType.Hand or PileType.Exhaust or PileType.None )
            await PowerCmd.Remove(this);
    }
}