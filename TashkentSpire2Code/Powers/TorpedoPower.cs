using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Extensions;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;
using TashkentSpire2.TashkentSpire2Code.Relics;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TorpedoPower : TashkentPower, IHasSecondAmount
{
    private const string TurnKey = "Turns";
    private const string BombKey = "IsTheBomb";
    private const int MaxVisibleTorpedoes = 12;
    private const int TorpedoColumns = 4;
    private const float TorpedoColumnSpacing = 72f;
    private const float TorpedoRowSpacing = 23f;
    private const float TorpedoRackVerticalOffset = 80f;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override string CustomBigIconPath =>
        "res://TashkentSpire2/images/powers/big/torpedo_power.png";

    public override string CustomPackedIconPath =>
        "res://TashkentSpire2/images/powers/packed/torpedo_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Unpowered),
        new DynamicVar(TurnKey, 0m),
        new DynamicVar(BombKey, 0m),
        new DynamicVar("AOEFlag", 0m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MarkPower>()
    ];
    
    private bool _oxygenApplied;
    private int? _lastDisplaySlot;
    private bool IsTheBomb => Owner != null && DynamicVars.ContainsKey(BombKey) && DynamicVars[BombKey].BaseValue == 1m;

    public override LocString Title
    {
        get
        {
            var stackTrace = new System.Diagnostics.StackTrace();
            string traceStr = stackTrace.ToString();
        
            if (traceStr.Contains("HoverTipFactory") || traceStr.Contains("DevConsole") || traceStr.Contains("CardLibrary"))
            {
                return base.Title;
            }

            try
            {
                if (IsTheBomb)
                {
                    return new LocString(locTable, Id.Entry + ".title_bomb");
                }
            }
            catch (System.Exception)
            {
                return base.Title;
            }

            return base.Title;
        }
    }

    public override int DisplayAmount => (int)DynamicVars[TurnKey].BaseValue;

    public string GetSecondAmount()
    {
        return Amount.ToString();
    }

    public void SetIsTheBomb(bool value)
    {
        DynamicVars[BombKey].BaseValue = value ? 1m : 0m;
        SyncAOEFlag();
    }

    public static int ComputeTurns(Creature owner)
    {
        var distPower = owner.GetPower<DistancePower>();
        int dist = distPower != null ? (int)distPower.Amount : 0;

        int baseTurns = 3;

        if (dist == 2 || dist == 3)
            baseTurns = 2;
        else if (dist >= 4)
            baseTurns = 1;
        else if (distPower == null || dist <= 1)
            baseTurns = 3;

        int godPowerBonus = (int)(owner.GetPower<TorpedoGodPower>()?.Amount ?? 0m);

        int relicBonus = owner.Player?.Relics.Count(r => r is Thruster) ?? 0;

        return Math.Max(1, baseTurns - godPowerBonus - relicBonus);
    }

    public void ReduceTurnCount(int amount)
    {
        var turnVar = DynamicVars[TurnKey];
        turnVar.BaseValue = Math.Max(1, turnVar.BaseValue - amount);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (!_oxygenApplied)
        {
            _oxygenApplied = true;

            int oxygenBonus = Owner?.GetPower<OxygenTorpedoPower>() is { } oxy? (int)oxy.Amount : 0;

            if (oxygenBonus != 0)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, oxygenBonus, Owner, cardSource);
            }
        }

        DynamicVars["AOEFlag"].BaseValue = IsTheBomb || Owner?.GetPower<TorpedoGodPower>() != null ? 1m : 0m;

        if (Owner != null && Owner.Player != null)
        {
            foreach (var device in Owner.Player.Relics.OfType<TorpedoRecoilDevice>())
            {
                await device.TryTriggerBlock(); 
            }
        }

        int turns = ComputeTurns(Owner!);
        DynamicVars[TurnKey].BaseValue = turns;
        InvokeDisplayAmountChanged();

        EnsureTorpedoVfx();
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        NTorpedoVfx? vfx = GetTorpedoVfx(oldOwner);
        int? vacatedSlot = vfx?.DisplaySlot ?? _lastDisplaySlot;
        vfx?.Dismiss();
        ReplenishVisibleTorpedoes(oldOwner, vacatedSlot);
        return Task.CompletedTask;
    }
    
    public void SyncAOEFlag()
    {
        DynamicVars["AOEFlag"].BaseValue = IsTheBomb || Owner?.GetPower<TorpedoGodPower>() != null ? 1m : 0m;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner))
            return;

        int turns = (int)DynamicVars[TurnKey].BaseValue;

        if (turns <= 0)
        {
            turns = ComputeTurns(Owner!);
            DynamicVars[TurnKey].BaseValue = turns;
            InvokeDisplayAmountChanged();
        }

        if (turns > 1)
        {
            turns--;
            DynamicVars[TurnKey].BaseValue = turns;
            InvokeDisplayAmountChanged();
            return;
        }

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var godPower = Owner?.GetPower<TorpedoGodPower>();
        bool isAOE = IsTheBomb || godPower != null;
        List<Creature> targets = isAOE
            ? enemies
            : SelectTarget(enemies) is { } selectedTarget
                ? [selectedTarget]
                : [];

        if (targets.Count == 0)
            return;

        Flash();

        // The first 0.3 s flight overlaps the power's original anticipation delay,
        // preserving its normal pacing. TorpedoGod then visits later targets in
        // strict settlement order, one 0.3 s flight at a time.
        Task firstFlight = LaunchTorpedoAtAsync(targets[0], targets.Count == 1);
        await Task.WhenAll(Cmd.CustomScaledWait(0.2f, 0.3f), firstFlight);

        for (int i = 0; i < targets.Count; i++)
        {
            Creature target = targets[i];
            if (i > 0)
            {
                await LaunchTorpedoAtAsync(target, i == targets.Count - 1);
            }

            PlayTheBombHitVfx(target);

            int markAmount = target.GetPower<MarkPower>()?.Amount ?? 0;
            var dmg = new DamageVar(Amount + markAmount, ValueProp.Unpowered);
            await CreatureCmd.Damage(choiceContext, target, dmg, Owner!);
        }

        var context = new TorpedoDamageContext
        {
            ChoiceContext = choiceContext,
            Source = Owner!,
            Targets = targets,
            Damage = Amount,
            IsBomb = IsTheBomb,
            IsAOE = isAOE
        };

        await TriggerAfterTorpedoDamage(choiceContext, context);

        await PowerCmd.Remove(this);
    }

    private NTorpedoVfx? EnsureTorpedoVfx(int? preferredSlot = null)
    {
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (ownerNode == null || Owner.IsDead)
        {
            return null;
        }

        NTorpedoVfx? existing = GetTorpedoVfx(Owner);
        if (existing is { CanLaunch: true })
        {
            _lastDisplaySlot = existing.DisplaySlot;
            return existing;
        }

        NTorpedoVfx[] currentVfx = ownerNode.GetChildren()
            .OfType<NTorpedoVfx>()
            .Where(node => node.CountsTowardDisplayLimit)
            .ToArray();
        if (currentVfx.Length >= MaxVisibleTorpedoes)
        {
            return null;
        }

        HashSet<int> occupiedSlots = currentVfx.Select(node => node.DisplaySlot).ToHashSet();
        int displaySlot = preferredSlot is >= 0 and < MaxVisibleTorpedoes
                          && !occupiedSlots.Contains(preferredSlot.Value)
            ? preferredSlot.Value
            : Enumerable.Range(0, MaxVisibleTorpedoes).First(slot => !occupiedSlots.Contains(slot));

        NTorpedoVfx? vfx = NTorpedoVfx.Create(this);
        if (vfx == null)
        {
            return null;
        }

        ownerNode.AddChildSafely(vfx);
        ownerNode.MoveChildSafely(vfx, 0);
        _lastDisplaySlot = displaySlot;
        vfx.BeginSpawn(ownerNode.VfxSpawnPosition, GetTorpedoRestPosition(ownerNode, displaySlot), displaySlot);
        return vfx;
    }

    private NTorpedoVfx? GetTorpedoVfx(Creature owner)
    {
        return NCombatRoom.Instance?.GetCreatureNode(owner)?.GetChildren()
            .OfType<NTorpedoVfx>()
            .FirstOrDefault(vfx => ReferenceEquals(vfx.Power, this));
    }

    private async Task LaunchTorpedoAtAsync(Creature target, bool isFinalTarget)
    {
        NTorpedoVfx? vfx = EnsureTorpedoVfx();
        var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);

        if (vfx != null && targetNode != null)
        {
            _lastDisplaySlot = vfx.DisplaySlot;
            await vfx.LaunchAsync(targetNode.VfxSpawnPosition, isFinalTarget);
            if (isFinalTarget)
            {
                // Impact is the first frame on which the old sprite is invisible,
                // so this is the earliest safe point to refill without exceeding 12.
                ReplenishVisibleTorpedoes(Owner!, _lastDisplaySlot, this);
            }
            return;
        }

        if (isFinalTarget)
        {
            vfx?.Dismiss();
        }

    }

    private static void PlayTheBombHitVfx(Creature target)
    {
        // This is the exact per-enemy effect used by the vanilla TheBombPower.
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(target));
    }

    private static Vector2 GetTorpedoRestPosition(NCreature ownerNode, int displaySlot)
    {
        Vector2 rackOrigin = ownerNode.GetBottomOfHitbox() + Vector2.Down * 25f;
        NHealthBar? healthBar = ownerNode.GetNodeOrNull<NHealthBar>("HealthBar/HealthBar");
        if (healthBar?.HpBarContainer is { } hpBar)
        {
            Rect2 rect = hpBar.GetGlobalRect();
            rackOrigin = new Vector2(
                rect.Position.X + rect.Size.X * 0.5f,
                rect.Position.Y + rect.Size.Y + 14f);
        }

        int column = displaySlot % TorpedoColumns;
        int row = displaySlot / TorpedoColumns;
        float centeredColumn = column - (TorpedoColumns - 1) * 0.5f;
        return rackOrigin + new Vector2(
            centeredColumn * TorpedoColumnSpacing,
            row * TorpedoRowSpacing + TorpedoRackVerticalOffset);
    }

    private static void ReplenishVisibleTorpedoes(
        Creature owner,
        int? preferredSlot,
        TorpedoPower? excludedPower = null)
    {
        if (owner.IsDead)
        {
            return;
        }

        foreach (TorpedoPower waitingPower in owner.Powers.OfType<TorpedoPower>())
        {
            if (ReferenceEquals(waitingPower, excludedPower))
            {
                continue;
            }

            NTorpedoVfx? existing = waitingPower.GetTorpedoVfx(owner);
            if (existing is { CanLaunch: true })
            {
                continue;
            }

            // Ensure enforces the global twelve-sprite ceiling. One successful
            // creation fills the single slot vacated by the triggering torpedo.
            waitingPower.EnsureTorpedoVfx(preferredSlot);
            return;
        }
    }

    private Creature? SelectTarget(List<Creature> enemies)
    {
        var enemyMarks = enemies.Select(e => new
            {
                Enemy = e,
                Mark = (int)(e.GetPower<MarkPower>()?.Amount ?? 0m)
            })
            .ToList();

        int maxMark = enemyMarks.Max(x => x.Mark);

        var candidates = enemyMarks
            .Where(x => x.Mark == maxMark)
            .Select(x => x.Enemy)
            .ToList();

        return candidates.Count > 0
            ? Owner?.Player?.RunState.Rng.CombatTargets.NextItem(candidates)
            : null;
    }

    private async Task TriggerAfterTorpedoDamage(PlayerChoiceContext choiceContext, TorpedoDamageContext context)
    {
        foreach (var hook in GetHooks<IAfterTorpedoDamage>())
        {
            await hook.AfterTorpedoDamage(choiceContext, context);
        }
    }

    private IEnumerable<T> GetHooks<T>()
    {
        foreach (var card in Owner!.Player!.Piles.SelectMany(p => p.Cards))
        {
            if (card is T t)
                yield return t;
        }
        
        foreach (var power in Owner.Powers)
        {
            if (power is T t)
                yield return t;
        }

        foreach (var relic in Owner.Player.Relics)
        {
            if (relic is T t)
                yield return t;
        }
    }
}
