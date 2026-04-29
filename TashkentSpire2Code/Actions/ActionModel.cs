using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Actions;

public abstract class ActionModel : PowerModel, ICustomPower
{
    public virtual TargetType TargetType => TargetType.None; 
    
    public virtual string? CustomPackedIconPath => null;
    public virtual string? CustomBigIconPath => null;

    public async Task<bool> TryAct(PlayerChoiceContext? choiceContext, Creature? _)
    {
        if (Owner is not { IsAlive: true } actor || actor.CombatState == null) 
            return false;

        await OnClick(actor, choiceContext);
        
        return true;
    }
    
    protected abstract Task OnClick(Creature actor, PlayerChoiceContext? context);
}