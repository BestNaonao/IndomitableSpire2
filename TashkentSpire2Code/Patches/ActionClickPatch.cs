using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Actions;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class MinionBodyClickPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        if (__instance.Hitbox != null)
        {
            __instance.Hitbox.Connect(Control.SignalName.GuiInput,
                Callable.From<InputEvent>(inputEvent => OnClick(__instance, inputEvent)));
        }
    }

    private static void OnClick(NCreature node, InputEvent ev)
    {
        if (node.GetViewport().IsInputHandled() || NTargetManager.Instance.IsInSelection) return;

        var creature = node.Entity;
        if (creature is null) return;

        var targetModel = creature.Powers.OfType<ActionModel>().FirstOrDefault();
        if (targetModel is not IClickableModel clickable) return;

        bool isLeft = ev is InputEventMouseButton { ButtonIndex: MouseButton.Left } leftBtn && leftBtn.IsReleased();
        bool isRight = ev is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightBtn && rightBtn.IsReleased();
        bool isController = ev is InputEventAction { Action: var act } actEv && act == MegaInput.cancel && actEv.IsPressed() && node.HasFocus();

        if (!isLeft && !isRight && !isController) return;

        var me = LocalContext.GetMe(creature.CombatState);
        if (me == null || (creature.Player != null && me.NetId != creature.Player.NetId)) return;

        string metaStr = isRight ? "RIGHT" : "LEFT";
        var context = new ClickContext(me, targetModel, new ClickContext.Payload(isController, metaStr));

        if (clickable.CanHandleClickLocal(context))
        {
            var queuedAction = new ClickCardAction(context, CombatManager.Instance.IsInProgress);
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(queuedAction);
            node.GetViewport().SetInputAsHandled();
        }
    }
}