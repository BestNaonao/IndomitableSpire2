using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using TashkentSpire2.TashkentSpire2Code.Minion;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class MinionCmd
{
    public static async Task<Creature> AddMinion<T>(Player player, MinionSummonOptions options = default)
        where T : MinionModel
    {
        ArgumentNullException.ThrowIfNull(player);

        var pet = await PlayerCmd.AddPet<T>(player);
        if (pet.Monster is MinionModel minionModel) minionModel.Position = options.Position;
        PetOrderSnapshotManager.TakeSnapshot(player);

        if (pet.Monster is MinionModel minion) await minion.OnSummon(player, pet, options);

        _ = MinionAnimCmd.Rearrange();

        return pet;
    }
}