using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Extensions;

public interface IAfterTorpedoDamage
{
    Task AfterTorpedoDamage(TorpedoDamageContext context);
}

public sealed class TorpedoDamageContext
{
    public required PlayerChoiceContext ChoiceContext { get; init; }

    public required Creature Source { get; init; }

    public required IReadOnlyList<Creature> Targets { get; init; }

    public required decimal Damage { get; init; }

    public required bool IsBomb { get; init; }

    public required bool IsAOE { get; init; }
}