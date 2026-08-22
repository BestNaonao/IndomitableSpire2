using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class ShikikanDakimakura : IndomitableRelic
{
    // 参考黑暗止血，升级后依然设为初始遗物
    public override RelicRarity Rarity => RelicRarity.Starter;
    
    // 定义动态变量：恢复 1 点生命值。参考了原版燃烧之血的写法。
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new HealVar(1M), 
        new MotivationGainVar(10M)
    ];
    
    private bool _gainExtraInNextCombat;
    // 保存状态：是否在上一层去过营火
    [SavedProperty]
    public bool GainExtraInNextCombat
    {
        get => _gainExtraInNextCombat;
        set
        {
            AssertMutable();
            if (_gainExtraInNextCombat == value) return;
            _gainExtraInNextCombat = value;
            Status = _gainExtraInNextCombat ? RelicStatus.Active : RelicStatus.Normal;
        }
    }
    
    // 监听进入房间：如果是休息处，打上标记
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) GainExtraInNextCombat = true;
        return Task.CompletedTask;
    }
    
    // 提取公共触发逻辑
    private async Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        await PowerCmd.Apply<MotivationPower>(
            choiceContext, Owner.Creature, DynamicVars.MotivationGain().BaseValue, Owner.Creature, null);
    }
    
    // 监听回合开始
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature.IsDead) return;
        await TriggerEffect(choiceContext);
        // 如果去了营火，并且是第一回合，额外触发一次
        if (GainExtraInNextCombat && player.PlayerCombatState?.TurnNumber == 1)
            await TriggerEffect(choiceContext);
    }
    
    // 监听回合结束
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side || Owner.Creature.IsDead || !participants.Contains(Owner.Creature)) return;
        await TriggerEffect(choiceContext);
        // 如果去了营火，并且是第一回合，额外触发一次
        if (GainExtraInNextCombat && Owner.PlayerCombatState?.TurnNumber == 1)
        {
            await TriggerEffect(choiceContext);
            GainExtraInNextCombat = false;
        }
    }
}