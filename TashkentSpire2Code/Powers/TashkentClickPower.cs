using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TashkentSpire2.TashkentSpire2Code.Patches;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public abstract class TashkentClickPower : TashkentPower, IClickableModel
{
    public virtual bool CanHandleClickLocal(ClickContext context)
    {
        return true;
    }

    public async Task OnClick(PlayerChoiceContext choiceContext, ClickContext clickContext)
    {
        if (clickContext.Extra.Meta == "LEFT")
        {
            await OnLeftClickSynchronized(choiceContext, clickContext);
        }
        else
        {
            await OnRightClickSynchronized(choiceContext, clickContext);
        }
    }

    protected virtual Task OnLeftClickSynchronized(PlayerChoiceContext choiceContext, ClickContext clickContext)
    {
        return Task.CompletedTask;
    }
    
    protected virtual Task OnRightClickSynchronized(PlayerChoiceContext choiceContext, ClickContext clickContext)
    {
        return Task.CompletedTask;
    }
}