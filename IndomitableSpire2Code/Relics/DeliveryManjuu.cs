using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enchantments;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class DeliveryManjuu : IndomitableSpire2Relic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Dessert>().Concat(HoverTipFactory.FromEnchantment<SampleEnchantment>());
    
    // 直接沿用原版限制，包含单人与多人楼层差异，不另写幕数/楼层常量。
    public override bool IsAllowed(IRunState runState) => IsBeforeAct3TreasureChest(runState);
    
    public override Task AfterCombatEnd(CombatRoom room)
    {
        Flash();
        return Task.CompletedTask;
    }
    
    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        // 参考“转经轮”“白星”：增加独立的卡牌奖励条目，不往原有三选一里插入甜点，每个遗物实例添加一份。
        // GenerateWithoutOffering 在读档后的奖励重建中也会调用，因而无需保存一个不支持序列化的指定牌奖励。
        if (player != Owner || room is not CombatRoom) return false;
        rewards.Add(new SampleCardReward<Dessert>(player));
        return true;
    }
    
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        // 训练假人等脚本战斗会跳过整个房间奖励界面，TryModifyRewards 根本不会执行。
        // 在牌堆清理完毕后单独提供可跳过的赠品，不给这类战斗凭空增加普通金币/卡牌奖励。
        // 领导者模式：多个实例由第一个统一开一次奖励界面，界面中每只送餐啾各提供一份奖励。
        var copies = Owner.Relics.OfType<DeliveryManjuu>()
            .Where(relic => !relic.IsMelted).ToList();
        if (!room.Encounter.ShouldGiveRewards && copies.FirstOrDefault() == this)
            await RewardsCmd.OfferCustom(Owner,
                copies.Select(Reward (_) => new SampleCardReward<Dessert>(Owner)).ToList());
    }
    
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // 参考“古茶具套装”：到达休息处就处理。
        if (room is not RestSiteRoom) return;
        var desserts = Owner.Deck.Cards
            .Where(card => card is Dessert && card.Enchantment is SampleEnchantment)
            .ToList();
        var removed = await CustomCardCmd.RemoveFromDeckWithRelicPreview(desserts, this);
        // 每移除一张就获得 3 点最大生命值。多只送餐啾增加供餐数量，但兑换比例固定。
        // 原版逐个 await 派发钩子：第一只清理完毕后，其余实例看见空列表，不会重复兑换。
        var maxHp = (int)(removed * DynamicVars.MaxHp.BaseValue);
        if (maxHp <= 0) return;
        Flash();
        // 同步最大生命值、治疗增加的生命值并记录历史，均交给原版命令。
        await CreatureCmd.GainMaxHp(Owner.Creature, maxHp);
    }
}