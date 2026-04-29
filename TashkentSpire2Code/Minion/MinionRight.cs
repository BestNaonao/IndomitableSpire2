using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using TashkentSpire2.TashkentSpire2Code.Actions;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Minion;

public sealed class MinionRight : MinionModel
{
    public override int MaxInitialHp => 1;
    public override int MinInitialHp => 1;

    protected override string VisualsPath => "res://TashkentSpire2/scenes/minions/minion_right.tscn";

    public const string IdleAnimName = "idle_loop";
    public const string DeathAnimName = "die";
    public const string BuffAnimName = "buff";
    public const string DebuffAnimName = "_ignore/hug2";
    public const string AttackAnimName = "attack";
    public const string PowerAttackAnimName = "debuff";
    public const string SleepAnimName = "_ignore/string_rigging";

    public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options)
    {
        var power = owner?.Creature.GetPower<GoneWithTheWindPower>();
        if (power != null)
        {
            await PowerCmd.Apply<MinionRightAction>(self, 1m, self, null, false);
        }
    }
}