using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim),
    typeof(Creature), typeof(string), typeof(float))]
internal static class TashkentAttackAudioContextPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Creature creature, string triggerName, out bool __state)
    {
        __state = TashkentAttackAudioTurnGate.TryEnter(creature, triggerName);
    }

    [HarmonyFinalizer]
    [HarmonyPriority(Priority.Last)]
    private static void Finalizer(bool __state)
    {
        if (__state)
            TashkentAttackAudioTurnGate.Exit();
    }
}

[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), typeof(string), typeof(float))]
internal static class TashkentAttackAudioPlaybackPatch
{
    [HarmonyPrefix]
    private static bool Prefix(string sfx)
    {
        return !string.Equals(sfx, TashkentCharacter.AttackVoicePath, StringComparison.Ordinal) ||
               TashkentAttackAudioTurnGate.ConsumeCurrentTurnPlay();
    }
}

// Presentation-only state: each player (including skin variants) has an independent
// allowance. No gameplay state, RNG, serialization or multiplayer messages are changed.
internal static class TashkentAttackAudioTurnGate
{
    [ThreadStatic]
    private static Stack<Player>? _attackPlayers;

    private static readonly ConditionalWeakTable<Player, PlayerTurnState> PlayerStates = new();

    internal static bool TryEnter(Creature creature, string triggerName)
    {
        if (!string.Equals(triggerName, "Attack", StringComparison.Ordinal) ||
            !creature.IsPlayer ||
            creature.Player is not { Character: TashkentCharacter } player)
            return false;

        // TriggerAnim emits SfxCmd.Play synchronously before its first await.
        // A stack also preserves the owner if another patch nests an attack call.
        (_attackPlayers ??= new Stack<Player>()).Push(player);
        return true;
    }

    internal static void Exit()
    {
        if (_attackPlayers is not { Count: > 0 })
            return;

        _attackPlayers.Pop();
        if (_attackPlayers.Count == 0)
            _attackPlayers = null;
    }

    internal static bool ConsumeCurrentTurnPlay()
    {
        if (_attackPlayers is not { Count: > 0 })
            return true; // Preserve explicit playback outside character attacks.

        Player player = _attackPlayers.Peek();
        PlayerCombatState? combatState = player.PlayerCombatState;
        if (combatState is null)
            return true;

        PlayerTurnState state = PlayerStates.GetValue(player, static _ => new PlayerTurnState());
        // CombatState identity resets the allowance between battles. Player turn
        // numbers, unlike RoundNumber, also account for individual extra turns.
        if (!ReferenceEquals(state.CombatState, combatState) || state.TurnNumber != combatState.TurnNumber)
        {
            state.CombatState = combatState;
            state.TurnNumber = combatState.TurnNumber;
            state.HasPlayed = false;
        }

        if (state.HasPlayed)
            return false;

        state.HasPlayed = true;
        return true;
    }

    private sealed class PlayerTurnState
    {
        internal PlayerCombatState? CombatState { get; set; }
        internal int TurnNumber { get; set; }
        internal bool HasPlayed { get; set; }
    }
}
