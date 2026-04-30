using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class OilCommission() : CommissionCard(TargetType.Self), IAfterEnergyGainedSubscriber
{
    protected override int InitialMaxProgressAmount => 6;
    
    // 添加能量变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("Progress", 0m),
        new EnergyVar("MaxProgress", InitialMaxProgressAmount),
        new StringVar("DelegatorName"),
        new EnergyVar(3)
    ];
    
    // 监听能量获取。这里采用无损拦截的方式，拦截能量然后返回原值。
    public Task AfterEnergyGained(Player player, decimal finalAmount)
    {
        if (player == Owner)
            AddProgress((int)finalAmount);
        return Task.CompletedTask;
    }
    
    protected override async Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["MaxProgress"].UpgradeValueBy(-1);
    }
}