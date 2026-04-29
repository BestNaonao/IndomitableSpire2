using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Actions;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class MinionBodyClickPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        __instance.Hitbox.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnClick(__instance, inputEvent)));
    }

    private static void OnClick(NCreature node, InputEvent ev)
    {
        if (ev is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn && btn.IsReleased())
        {
            var action = node.Entity.Powers.OfType<ActionModel>().FirstOrDefault();
            if (action != null)
            {
                _ = action.TryAct(null, null); 
                node.GetViewport().SetInputAsHandled();
            }
        }
    }
}

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
public static class MinionIconClickPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        __instance.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnClick(__instance, inputEvent)));
    }

    private static void OnClick(NPower node, InputEvent ev)
    {
        if (ev is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn && btn.IsReleased())
        {
            if (node.Model is ActionModel action)
            {
                _ = action.TryAct(null, null);
                node.GetViewport().SetInputAsHandled();
            }
        }
    }
}