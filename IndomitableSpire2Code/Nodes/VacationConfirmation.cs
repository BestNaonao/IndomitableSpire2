using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>嵌在本机地图内的模态层，复用原版 VerticalPopup 与取消/确认按钮的场景、材质和字体。</summary>
public partial class VacationConfirmation : Control
{
    private const string PackedScenePath = "res://scenes/ui/vertical_popup.tscn";
    private static readonly string LocEntryKeyPrefix = ModelDb.Relic<VacationProposal>().Id.Entry;
    
    // 静态工厂方法，一键完成构造、挂载和装配，复用原版场景。
    public static VacationConfirmation Show(NMapScreen map, Action confirm, Action cancel)
    {
        var overlay = new VacationConfirmation { Name = "VacationConfirmation", MouseFilter = MouseFilterEnum.Stop };
        map.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = ResourceLoader.Load<PackedScene>(PackedScenePath).Instantiate<NVerticalPopup>();
        overlay.AddChild(panel);
        panel.MouseFilter = MouseFilterEnum.Stop; // 阻挡鼠标事件穿透
        panel.SetText(new LocString("relics", LocEntryKeyPrefix + ".title"), 
            new LocString("relics", LocEntryKeyPrefix + ".confirm"));
        panel.InitYesButton(new LocString("relics", LocEntryKeyPrefix + ".yes"), _ => confirm());
        panel.InitNoButton(new LocString("relics", LocEntryKeyPrefix + ".cancel"), _ => cancel());
        // 解除原版信号绑定。原版 Init*Button 还会连接 NModalContainer.Clear。
        // 这里不是全局模态框，必须解除那组回调，换成只关闭本地图子层的回调，不能误清理其他弹窗。
        panel.DisconnectSignals();
        // 重新绑定安全的本地信号
        panel.YesButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => confirm()));
        panel.NoButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => cancel()));
        // 处理点击遮罩层外部区域 (左键点击空白处关闭)
        overlay.GuiInput += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
            overlay.AcceptEvent(); // 告诉 Godot 引擎“这个输入事件我已经处理完毕，停止向上层节点冒泡”，防止触发地图的点击逻辑。
            cancel();
        };
        return overlay;
    }
}