using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Actions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>
/// 挂载在 NCombatUi 下的本地“认真模式”按钮。
/// 与 NEndTurnButton 一样派生自 NButton，通过 Enable/Disable 和 OnRelease 管理交互。
/// </summary>
[GlobalClass]
public partial class EarnestModeButton : NButton
{
    private const string LocKey = "INDOMITABLESPIRE2-EARNEST_MODE_BUTTON";
    private static EarnestModeButton? _instance;
    
    private CombatState? _combatState;
    private NCombatUi _combatUi = null!;
    private Control _visuals = null!;
    private TextureRect _image = null!;
    private TextureRect _glow = null!;
    private MegaLabel _label = null!;
    private bool _requestPending;   // 异步请求的状态锁，为 true 时表示客户端已发送激活请求，正在等待服务端/游戏逻辑处理完毕
    
    public override void _Ready()
    {
        _instance = this;
        _combatUi = GetParent<NCombatUi>();
        _visuals = GetNode<Control>("Visuals");
        _image = GetNode<TextureRect>("Visuals/Image");
        _glow = GetNode<TextureRect>("Visuals/Glow");
        _label = GetNode<MegaLabel>("Visuals/Label");
        
        // NButton 的派生类不能调用 base._Ready()；原版按钮统一直接连接这些信号。
        ConnectSignals();
        
        _label.SetTextAutoSize(new LocString("gameplay_ui", LocKey).GetFormattedText());
        TooltipText = new LocString("gameplay_ui", $"{LocKey}.description").GetFormattedText();
        
        Disable();
        Hide();
    }
    
    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
        base._ExitTree();
    }
    
    public override void _Process(double delta)
    {
        // 回合阶段、选牌遮罩与多人 ready 状态不一定触发能力钩子，因此逐帧做轻量只读刷新。
        RefreshState();
    }
    
    public void Initialize(CombatState combatState)
    {
        _combatState = combatState;
        _requestPending = false;
        RefreshState();
    }
    
    // 供外部安全触发的状态刷新代理。
    public static void RequestRefresh()
    {
        if (IsInstanceValid(_instance))
            _instance.RefreshState();
    }
    
    // 异步请求完成的回调通知。_requestPending 重置为 false，解除锁定以允许按钮设置为可交互
    public static void NotifyActionResolved()
    {
        if (!IsInstanceValid(_instance)) return;
        _instance._requestPending = false;
        _instance.RefreshState();
    }
    
    protected override void OnPress()
    {
        if (!CanRequestActivation(out _)) return;
        base.OnPress();
        _visuals.Position = new Vector2(0f, 5f);
    }
    
    protected override void OnRelease()
    {
        if (!CanRequestActivation(out var me))
        {
            RefreshState();
            return;
        }
        _requestPending = true;
        Disable();
        _visuals.Position = Vector2.Zero;
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new EarnestModeButtonAction(me));
    }
    
    protected override void OnFocus()
    {
        base.OnFocus();
        _visuals.Position = new Vector2(0f, -2f);
        _label.Modulate = Colors.Cyan;
        _glow.Modulate = new Color(1f, 1f, 1f, 0.8f);
    }
    
    protected override void OnUnfocus()
    {
        _visuals.Position = Vector2.Zero;
        _label.Modulate = IsEnabled ? StsColors.cream : StsColors.gray;
        _glow.Modulate = Colors.Transparent;
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _image.Modulate = Colors.White;
        _label.Modulate = StsColors.cream;
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        _image.Modulate = StsColors.gray;
        _label.Modulate = StsColors.gray;
        _glow.Modulate = Colors.Transparent;
        _visuals.Position = Vector2.Zero;
    }
    
    /// <summary>
    /// 刷新按钮状态。在战斗外隐藏；在战斗内本地玩家有全神贯注能力的回合内显示，并且满足激活条件时可交互。
    /// </summary>
    private void RefreshState()
    {
        // 1. 边界条件：非战斗状态或无战斗上下文时，直接隐藏并禁用按钮
        if (_combatState is null || !CombatManager.Instance.IsInProgress)
        {
            Hide();
            Disable();
            return;
        }
        
        // 2. 获取本地玩家及关键能力状态
        var me = LocalContext.GetMe(_combatState);
        var concentration = me?.Creature.GetPower<FullConcentrationPower>();
        var earnestMode = me?.Creature.GetPower<EarnestModePower>();
        
        // 3. 异常回退机制（防卡死）：
        // 如果当前处于“请求已发送但未响应”的等待状态，但游戏状态已发生不可逆变化（例如：已经成功获得了认真模式，或者全神贯注能力丢失/不可用），
        // 则说明之前的请求已失效或已被其他方式处理，强制解除 pending 状态以避免按钮永久禁用。
        if (_requestPending && (earnestMode is not null || concentration is not { IsActivationAvailable: true }))
            _requestPending = false;
        
        // 4. 只查询本地玩家自己的生物；其他玩家即使拥有全神贯注，也不会在本客户端显示按钮。
        var shouldShow = me is not null && concentration?.Owner.Player == me && earnestMode is null && 
                         CombatManager.Instance.IsPartOfPlayerTurn(me);
        Visible = shouldShow;
        SetEnabled(shouldShow && CanRequestActivation(out _));
    }
    
    /// <summary>
    /// 核心安全校验网关。判断是否满足请求激活认真模式的条件。
    /// </summary>
    private bool CanRequestActivation(out Player player)
    {
        player = null!;
        if (_combatState is null || _requestPending || !Visible) return false;
        
        var me = LocalContext.GetMe(_combatState);
        var concentration = me?.Creature.GetPower<FullConcentrationPower>();
        var combatRoom = NCombatRoom.Instance;
        if (me is null || concentration?.Owner.Player != me || combatRoom is null ||
            !ActiveScreenContext.Instance.IsCurrent(combatRoom) ||
            _combatUi.Hand.IsInCardSelection || _combatUi.Hand.InCardPlay ||
            NTargetManager.Instance.IsInSelection ||
            !concentration.CanEnterEarnestMode(me))
        {
            return false;
        }
        
        player = me;
        return true;
    }
}