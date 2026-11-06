using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;

public sealed class EyeInTheSky : ImplacableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    public override bool HasUponPickupEffect => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new ReconVar(2), new MaxHpVar(1), 
        new("Spread", EyeInTheSkyService.SpreadLimit)
    ];
    
    public override Task AfterObtained()
    {
        // 拾起时也为当前阶段投放；服务对阶段去重，多人和重复遗物不会重复投放。
        EyeInTheSkyService.EnsureMap(Owner.RunState, Owner.RunState.Map, Owner.RunState.CurrentActIndex);
        return Task.CompletedTask;
    }
    
    public override Task AfterMapGenerated(ActMap map, int actIndex)
    {
        // 每个阶段地图重新生成时，也需要进行标记。
        EyeInTheSkyService.EnsureMap(Owner.RunState, map, actIndex);
        return Task.CompletedTask;
    }
    
    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        // 在任何一件空中之眼开始扩散前，保存这次进入的旧标记状态。
        EyeInTheSkyService.BeforeEnter(Owner.RunState, room);
        return Task.CompletedTask;
    }
    
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        var run = Owner.RunState;
        if (!Owner.Relics.Contains(this) || !EyeInTheSkyService.TryBeginRelicEntry(run, room, this, out var marked))
            return;
        // 每件遗物只处理自己的代价。所有实例读取同一份进入前快照，因此即使第一件已扩散，后续实例也不会追溯扣血。
        if (marked)
        {
            Flash();
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature,
                DynamicVars.MaxHp.BaseValue, isFromCard: false);
        }
        EyeInTheSkyService.SpreadAfterEntry(run, room);
    }
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        Flash();
        await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
    }
}