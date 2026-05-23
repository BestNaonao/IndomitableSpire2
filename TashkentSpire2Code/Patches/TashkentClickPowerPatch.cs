using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

public record ClickContext(Player Player, AbstractModel Model, ClickContext.Payload Extra = default)
{
    public struct Payload : IPacketSerializable
    {
        public bool IsController { get; private set; }
        public string? Meta { get; private set; }

        public Payload(bool isController = false, string? meta = null)
        {
            IsController = isController;
            Meta = meta;
        }

        public void Serialize(PacketWriter writer)
        {
            writer.WriteBool(IsController);
            writer.WriteBool(Meta != null);
            if (Meta != null)
                writer.WriteString(Meta);
        }

        public void Deserialize(PacketReader reader)
        {
            IsController = reader.ReadBool();
            Meta = reader.ReadBool() ? reader.ReadString() : null;
        }
    }
}

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
public static class TashkentClickPowerPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        __instance.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnPowerGuiInput(__instance, inputEvent)));
    }

    private static void OnPowerGuiInput(NPower powerNode, InputEvent inputEvent)
    {
        if (powerNode.GetViewport().IsInputHandled() || NTargetManager.Instance.IsInSelection) return;

        var modelField = AccessTools.Field(typeof(NPower), "_model");
        if (modelField.GetValue(powerNode) is not TashkentClickPower power) return; 

        bool isLeft = inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } leftBtn && leftBtn.IsReleased();
        bool isRight = inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightBtn && rightBtn.IsReleased();
        bool isController = inputEvent is InputEventAction { Action: var act } actEv && act == MegaInput.cancel && actEv.IsPressed() && powerNode.HasFocus();

        if (!isLeft && !isRight && !isController) return;

        var me = LocalContext.GetMe(power.Owner.CombatState);
        if (me == null || (power.Owner.Player != null && me.NetId != power.Owner.Player.NetId)) return;

        var context = new ClickContext(me, power, new ClickContext.Payload(isController, isLeft ? "LEFT" : "RIGHT"));

        if (power.CanHandleClickLocal(context))
        {
            var queuedAction = new ClickCardAction(context, CombatManager.Instance.IsInProgress);
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(queuedAction);
            powerNode.GetViewport().SetInputAsHandled();
        }
    }
}

public interface IClickableModel
{
    bool CanHandleClickLocal(ClickContext context) => true;
    Task OnClick(PlayerChoiceContext choiceContext, ClickContext clickContext);
}

public class ClickCardAction : GameAction
{
    public Player Player { get; }
    public ClickContext.Payload Extra { get; }
    public bool WasEnqueuedInCombat { get; }
    public ModelId ModelId { get; }
    public uint CreatureCombatId { get; }
    
    public override ulong OwnerId => Player.NetId;
    public override GameActionType ActionType => WasEnqueuedInCombat ? GameActionType.CombatPlayPhaseOnly : GameActionType.NonCombat;

    public ClickCardAction(ClickContext context, bool isCombatInProgress)
    {
        WasEnqueuedInCombat = isCombatInProgress;
        Player = context.Player;
        Extra = context.Extra;
        ModelId = context.Model.Id;

        if (context.Model is PowerModel power && power.Owner.CombatId != null)
        {
            CreatureCombatId = power.Owner.CombatId.Value;
        }
    }

    public ClickCardAction(Player player, ModelId modelId, uint creatureCombatId, ClickContext.Payload extra, bool isCombatInProgress)
    {
        Player = player;
        ModelId = modelId;
        CreatureCombatId = creatureCombatId;
        Extra = extra;
        WasEnqueuedInCombat = isCombatInProgress;
    }

    protected override async Task ExecuteAction()
    {
        var combatState = Player.Creature.CombatState;
        if (WasEnqueuedInCombat && combatState is null) return;

        var model = combatState!.GetCreature(CreatureCombatId)?.Powers.FirstOrDefault(p => p.Id == ModelId);
        if (model is not IClickableModel clickable) return;

        var choiceContext = new GameActionPlayerChoiceContext(this);
        await clickable.OnClick(choiceContext, new ClickContext(Player, model, Extra));
        model.InvokeExecutionFinished();
    }

    public override INetAction ToNetAction()
    {
        return new NetClickCardAction
        {
            ModelId = ModelId,
            CreatureCombatId = CreatureCombatId,
            Extra = Extra,
            WasEnqueuedInCombat = WasEnqueuedInCombat
        };
    }
}

public struct NetClickCardAction : INetAction
{
    public ModelId ModelId;
    public uint CreatureCombatId;
    public ClickContext.Payload Extra;
    public bool WasEnqueuedInCombat;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFullModelId(ModelId);
        writer.WriteUInt(CreatureCombatId);
        writer.Write(Extra);
        writer.WriteBool(WasEnqueuedInCombat);
    }

    public void Deserialize(PacketReader reader)
    {
        ModelId = reader.ReadFullModelId();
        CreatureCombatId = reader.ReadUInt();
        Extra = reader.Read<ClickContext.Payload>();
        WasEnqueuedInCombat = reader.ReadBool();
    }

    public GameAction ToGameAction(Player player)
    {
        return new ClickCardAction(player, ModelId, CreatureCombatId, Extra, WasEnqueuedInCombat);
    }
}