using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class TorpedoRecoilDevice : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/TorpedoRecoilDevice.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/TorpedoRecoilDevice.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/TorpedoRecoilDevice.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<TorpedoPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7M, ValueProp.Unpowered)];
    
    private int _triggered = 0;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == this.Owner?.Creature.Player)
        {
            _triggered = 0;
        }
        return Task.CompletedTask;
    }
    
    public async Task<bool> TryTriggerBlock()
    {
        await _lock.WaitAsync();
        try
        {
            if (_triggered == 0)
            {
                _triggered = 1;
                Flash();

                int blockAmount = (int)DynamicVars.Block.BaseValue;
                if (Owner.Creature.Player != null)
                {
                    await CreatureCmd.GainBlock(Owner.Creature.Player.Creature, blockAmount, ValueProp.Unpowered, null);
                }
                return true;
            }
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }
}