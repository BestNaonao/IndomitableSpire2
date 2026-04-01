using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class DistancePower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override bool IsInstanced => true;
    
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/hypnotized_power.png";
    
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/hypnotized_power_packed.tres";
    
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int clamped = Mathf.Clamp((int)base.Amount, -5, 5);
        
        base.Amount = clamped;

        int delta = clamped;

        await UpdateCreaturePositions(delta);
    }
    
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal oldAmount, Creature? __, CardModel? cardSource)
    {
        if (power == this)
        {
            int delta = (int)(base.Amount - oldAmount);
            await UpdateCreaturePositions(delta);
            if (LocalContext.IsMe(base.Target))
            {
                int num = Mathf.Clamp(Math.Abs((int)base.Amount), 0, 5);
                NRunMusicController.Instance?.UpdateMusicParameter(TheInsatiable.TheInsatiableTrackName, num);
            }
        }
    }
    
    private List<Creature> GetOwnerAndPets()
    {
        List<Creature> result = new List<Creature>();
        
        result.Add(base.Owner);

        if (base.Owner.Pets != null)
        {
            foreach (var pet in base.Owner.Pets)
            {
                result.Add(pet);
            }
        }

        return result;
    }
    private async Task UpdateCreaturePositions(int delta)
    {
        if (delta == 0) return;

        float moveDistance = delta * 50f;

        Tween? tween = null;

        foreach (Creature creature in GetOwnerAndPets())
        {
            if (creature.IsDead) continue;

            NCreature? node = NCombatRoom.Instance.GetCreatureNode(creature);
            if (node == null) continue;

            if (tween == null)
            {
                tween = NCombatRoom.Instance.CreateTween()
                    .SetParallel()
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
            }

            tween.TweenProperty(
                node,
                "global_position:x",
                node.GlobalPosition.X + moveDistance,
                0.25f
            );
        }

        if (tween != null)
        {
            await tween.ToSignal(tween, Tween.SignalName.Finished);
        }
    }
}