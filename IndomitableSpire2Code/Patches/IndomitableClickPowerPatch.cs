using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Actions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NPower), nameof(NPower._Ready))]
public static class IndomitableClickPowerPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        // 为新生成的原版 NPower 视觉节点挂载输入监听
        __instance.Connect(Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnPowerGuiInput(__instance, inputEvent)));
    }

    private static void OnPowerGuiInput(NPower powerNode, InputEvent inputEvent)
    {
        // 如果 UI 事件已经被其他层（比如选牌网格）拦截，则忽略
        if (powerNode.GetViewport().IsInputHandled() || NTargetManager.Instance.IsInSelection) return;
        
        // 反射获取 NPower 绑定的逻辑模型
        var modelField = AccessTools.Field(typeof(NPower), "_model");
        
        // 【防弹衣级兼容性判定】：如果这个能力没有实现你的专属接口，立刻退出！
        // 这样绝不会拦截原版能力，也不会拦截 Tashkent Mod 的能力。
        if (modelField.GetValue(powerNode) is not IIndomitableClickablePower powerModel) return;
        var powerLogicModel = powerNode.Model;
        // 检测输入设备
        var isLeft = inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left } leftBtn && leftBtn.IsReleased();
        var isRight = inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightBtn && rightBtn.IsReleased();
        var isController = inputEvent is InputEventAction { Action: var act } actEv && act == MegaInput.cancel && actEv.IsPressed() && powerNode.HasFocus();

        if (!isLeft && !isRight && !isController) return;

        // 获取当前操作的本地玩家
        var me = LocalContext.GetMe(powerLogicModel.Owner.CombatState);
        
        // 防止跨玩家点击（如果该能力属于玩家，只能由该玩家本人点击）
        // 备注：如果能力属于怪物，所有人都可以尝试发送点击请求，具体放行逻辑由 CanHandleClickLocal 决定。
        if (me == null || (powerLogicModel.Owner.Player != null && me.NetId != powerLogicModel.Owner.Player.NetId)) return;

        // 生成上下文
        var context = new IndomitableClickContext(me, powerLogicModel, new IndomitableClickContext.Payload(isController, isLeft ? "LEFT" : "RIGHT"));
        
        // 调用本地预判
        if (powerModel.CanHandleClickLocal(context))
        {
            // 向服务器队列发送点击事件！
            var queuedAction = new IndomitableClickPowerAction(context, CombatManager.Instance.IsInProgress);
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(queuedAction);
            
            // 将输入标记为已处理，防止事件穿透到下层 UI
            powerNode.GetViewport().SetInputAsHandled();
        }
    }
}