using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Actions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>UI 入口只负责输入与所有权；本机预览和联机游戏行动在统一分发器中分开。</summary>
[HarmonyPatch]
public static class IndomitableClickPatch
{
    private const string Connected = "indomitable_click_connected";
    
    [HarmonyPatch(typeof(NPower), nameof(NPower._Ready)), HarmonyPostfix]
    private static void Power(NPower __instance) => Connect(__instance, () => __instance.Model);
    
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready)), HarmonyPostfix]
    private static void Relic(NRelicInventoryHolder __instance) => Connect(__instance, () => __instance.Relic.Model);
    
    private static void Connect(Control node, Func<AbstractModel> model)
    {
        if (node.HasMeta(Connected)) return;
        node.SetMeta(Connected, true); // 标记已经连接，避免重复绑定
        // 特殊处理遗物：保留原版的 MouseReleased 信号配对逻辑。
        if (node is NRelicInventoryHolder)
            node.Connect(NClickableControl.SignalName.MouseReleased,
                Callable.From<InputEvent>(input => Dispatch(node, model(), input)));
        // 通用 GUI 输入监听：捕获鼠标或手柄输入。能力或手柄的特殊输入，分发给 Dispatch 处理。
        node.GuiInput += input =>
            { if (node is NPower || input is InputEventAction) Dispatch(node, model(), input); };
    }
    
    private static void Dispatch(Control node, AbstractModel model, InputEvent input)
    {
        // 判断是否为手柄输入：动作是 "cancel" (通常映射为右键/B键)，且是按下瞬间，非系统重复事件，且当前节点拥有焦点。
        var controller = input is InputEventAction { Action: var action, Pressed: true } &&
                         action == MegaInput.cancel && !input.IsEcho() && node.HasFocus();
        // 只处理有效手柄输入或鼠标右键释放。只处理契约模型、未处理事件和不处于当前正选择状态的对象。
        if (!controller && input is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }) return;
        if (model is not IIndomitableClickableModel clickable || node.GetViewport().IsInputHandled() ||
            NRun.Instance?.GlobalUi.TargetManager.IsInSelection == true) return;
        var owner = model switch { RelicModel r => r.Owner, PowerModel p => p.Owner.Player, _ => null };
        var player = model switch
        {
            RelicModel r => LocalContext.GetMe(r.Owner.RunState),
            PowerModel p => LocalContext.GetMe(p.Owner.CombatState),
            _ => null
        };
        if (player == null || owner != null && player != owner) return; // 防止操作别人的遗物、能力
        var context = new IndomitableClickContext(player, model, new IndomitableClickContext.Payload(controller));
        if (!clickable.CanHandleClickLocal(context)) return;
        if (!clickable.SynchronizeClick) // 分支 A: 不需要网络同步（如纯本地 UI 预览），直接安全地异步执行。
            TaskHelper.RunSafely(clickable.OnClick(null, context));
        else if (model is PowerModel) // 分支 B: 需要同步，且是能力模型。将其封装为 Action 并请求加入同步队列。
            RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new IndomitableClickPowerAction(context, CombatManager.Instance.IsInProgress));
        else return; // 分支 C: 遗物的正式提交使用带实例编号的专用行动；不能按 ModelId 定位重复遗物。
        node.GetViewport().SetInputAsHandled();
    }
}