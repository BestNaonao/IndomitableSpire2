using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TashkentSpire2.TashkentSpire2Code.Patches;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Actions;

public abstract class GoneWithTheWindAction : ActionModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override TargetType TargetType => TargetType.None; 
    
    protected override async Task OnClick(Creature actor, PlayerChoiceContext? context)
    {
        var ownerPlayer = actor.PetOwner;
        var player = ownerPlayer?.Creature;
        if (ownerPlayer == null || player == null || !ClickActionEligibility.CanActThisTurn(ownerPlayer)) return;
        
        var power = player.GetPower<GoneWithTheWindPower>();
        if (power == null) return;
        
        if (!CanExecute(player))
        {
            return;
        }
        
        if (await power.TryConsumeCharge())
        {
            await ExecuteEffect(context, actor);
        }
    }

    protected abstract bool CanExecute(Creature player);
    
    protected abstract Task ExecuteEffect(PlayerChoiceContext? context, Creature self);
}
