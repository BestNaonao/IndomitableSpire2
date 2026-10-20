using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public interface IAfterReconSubscriber
{
    Task AfterRecon(PlayerChoiceContext choiceContext, Player player);
}