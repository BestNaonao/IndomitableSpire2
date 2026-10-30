using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class SeaFuryFb11() : VersatileAircraftCard(0, CardRarity.Rare)
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new SeaFuryFighter(), new SeaFuryBombs()]);
    
    public override IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    protected override int MaxDurability { get; set; } = 9;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    protected override (CardTag Tag, CardKeyword Keyword) TypeForOption(string id) => id == SeaFuryFighter.OptionId
        ? (IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter)
        : (IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber);
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
        [..this.CreateVersatileVars(), new CustomPowerVar<InternationalOrdersPower>(5M)];
    
    /// <summary>本次海怒交付的出口方；不沿 CloneOf 追溯，也不随通用克隆继承。</summary>
    private Player? Exporter { get; set; }
    private bool HasPlacedOrder { get; set; }
    
    // 预览和实际下单复用同一条件；查询本身不锁定订单，也不沿 CloneOf 查找出口方。
    private Player? OrderRecipient => CombatState != null && !HasPlacedOrder && Exporter is { } exporter &&
                                      exporter != Owner && exporter.Creature.IsAlive &&
                                      exporter.RunState == Owner.RunState &&
                                      exporter.Creature.CombatState == CombatState ? exporter : null;
    
    protected override async Task BeforeLoadout(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一次实际打出即锁定这份订单，先于攻击，确保最后一击仍可记入战后收益。
        if (HasPlacedOrder) return;
        var recipient = OrderRecipient;
        HasPlacedOrder = true;
        if (recipient == null) return;
        await PowerCmd.Apply<InternationalOrdersPower>(choiceContext, recipient.Creature,
            DynamicVars["InternationalOrdersPower"].BaseValue, Owner.Creature, this);
    }
    
    protected override async Task AfterLoadout(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        var allies = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature is { IsPlayer: true, IsAlive: true } && creature.Player != Owner)
            .Select(creature => creature.Player!)
            .Distinct().ToArray();
        foreach (var ally in allies)
        {
            var copy = (SeaFuryFb11)CreateCloneForPlayer(ally);
            // 必须覆写商业来源。B 打出 A 交付的 X 时，新交付的 Z 的出口方是 B，不是 A。
            copy.Exporter = Owner;
            copy.HasPlacedOrder = false;
            // 创建者是出牌者，所有权是接收者；自拍等生成钩子归属创建者。
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random));
        }
    }
    
    // 防止通过其他手段复制的复制品给予相同的出口方国际订单
    protected override void AfterCloned()
    {
        base.AfterCloned();
        Exporter = null;
        HasPlacedOrder = false;
    }
    
    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["InternationalOrdersPower"].UpgradeValueBy(2M);
    }
}