using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Animation;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>随战斗角色或皮肤预览节点创建和销毁的本地空闲演出，不参与战斗状态或网络同步。</summary>
public partial class IndomitableIdleAnimation : Node
{
    private const string NodeName = "IndomitableIdleAnimation";
    private const string NormalAnimation = "normal";
    private const string AnimationName = "motou";
    private const string TouchAnimation = "touch";
    private const string HoldAnimation = "tuozhuai";
    private const ulong LongPressMilliseconds = 500;
    private readonly IdleAnimationTimer _timer = new();
    // 全代码双分支的开关。不为 null 时动画通过走游戏自身的动画系统，为 null 时直接操作 Spine 的 MegaAnimationState。
    private NCreature? _creature;
    private CanvasItem _visual = null!;
    private MegaSprite _sprite = null!;
    private MegaAnimationState? _animationState;
    private Callable _animationChanged;
    private Control? _inputBounds; // 接收鼠标输入的命中区域；为 null 表示交互功能关闭。
    private Control.MouseFilterEnum _originalMouseFilter; // 保存 Bounds 原本的 MouseFilter，退出时恢复。
    private ulong? _pressStartedAt; // 左键按下的时刻；为 null 表示未按下。
    private bool _playingHold; // 长按动画是否"由本节点播放"，决定松开时是否由本节点负责恢复。
    
    // 战斗角色挂载入口
    internal static void Attach(NCreature creature, bool enableModelInput = false)
    {
        if (creature.Entity.Player?.Character is not Indomitable || CreatureAnimation.GetAnimator(creature) == null ||
            creature.Visuals.SpineBody is not { } sprite) return;
        AttachToVisual(creature, sprite, enableModelInput, creature);
    }
    
    // 预览角色挂载入口
    internal static void AttachPreview(Node2D visual, MegaSprite sprite, bool enableModelInput = false)
    {
        if (!sprite.HasAnimation(NormalAnimation)) return;
        AttachToVisual(visual, sprite, enableModelInput);
    }
    
    // 公共挂载逻辑
    private static void AttachToVisual(CanvasItem visual, MegaSprite sprite, bool enableModelInput, NCreature? creature = null)
    {
        if (!sprite.HasAnimation(AnimationName) || visual.GetNodeOrNull<IndomitableIdleAnimation>(NodeName) != null)
            return;
        visual.AddChild(new IndomitableIdleAnimation
        {
            Name = NodeName, _visual = visual, _sprite = sprite, _creature = creature,
            _inputBounds = enableModelInput
                ? creature?.Visuals.Bounds ?? visual.GetNodeOrNull<Control>("%Bounds") // 优先取原版的边界
                : null
        });
    }
    
    // 对外暴露播放一次“motou”动画的接口
    internal static bool TryPlay(NCreature creature) => TryPlayCreatureAnimation(creature, AnimationName, loop: false);
    
    private static bool TryPlayCreatureAnimation(NCreature creature, string name, bool loop)
    {
        // 按实际 Spine 动作判断，不区分定时器或 Ping 来源；重复 Ping 只显示气泡，不重启动画。
        if (creature.Entity is not { IsAlive: true, Player.Character: Indomitable } || !CreatureAnimation
                .TryResolveSpineAnimation(creature, name, out var trigger, isLooping: loop, animationGroup: "Idle") ||
            creature.SpineAnimation.GetCurrentAnimationName() == name) return false;
        creature.SetAnimationTrigger(trigger);
        return true;
    }
    
    public override void _EnterTree()
    {
        _animationState = _sprite.GetAnimationState();
        _animationChanged = Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationChanged);
        // 订阅 Spine 动画开始/结束信号——计时器由真实轨道事件驱动，而非轮询。
        _sprite.ConnectAnimationStarted(_animationChanged);
        _sprite.ConnectAnimationEnded(_animationChanged);
        _visual.VisibilityChanged += OnVisibilityChanged; // 订阅宿主可见性变化
        if (_inputBounds != null)
        {
            // 开启互动时：先存原值，再改成 Pass——控件能收到 _gui_input，但未处理的事件会继续向父级冒泡，不像 Stop 那样自动吞掉一切输入。
            _originalMouseFilter = _inputBounds.MouseFilter;
            _inputBounds.MouseFilter = Control.MouseFilterEnum.Pass;
            // 订阅 GUI 输入；订阅窗口失焦 → 取消按压，防止"按住时切窗口"导致长按状态卡死。
            _inputBounds.GuiInput += OnModelInput;
            GetWindow().FocusExited += CancelModelPress;
        }
        OnVisibilityChanged();
    }
    
    private void OnAnimationChanged(GodotObject _, GodotObject __, GodotObject ___) => ObserveCurrentAnimation();
    
    private void ObserveCurrentAnimation() => _timer.ObserveAnimation(_animationState?.GetCurrentAnimationName());
    
    private void OnVisibilityChanged()
    {
        // 面板即使在两帧间隐藏又显示，也不能沿用隐藏前累计的待机时间。
        if (_visual.IsVisibleInTree()) ObserveCurrentAnimation();
        else
        {
            CancelModelPress(); // 取消按压/长按并恢复动画
            _timer.ObserveAnimation(null);
        }
    }
    
    // 鼠标状态机
    private void OnModelInput(InputEvent input)
    {
        // 只处理左键的按下/释放事件并捕获为 button
        if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left } buttonEvent) return;
        if (buttonEvent.IsCanceled())
        {
            CancelModelPress(); // 按键事件被（系统）取消，按照“按压中止”处理
            return;
        }
        // 处理按钮事件，分为按下事件和释放事件，按下时记录时间戳，松开时触发“touch”动画。
        if (buttonEvent.Pressed)
        {
            // GuiInput 的坐标本就相对该控件原点，所以用 (0,0,Size) 的矩形校验按下点；不可见或点在区域外 → 直接 return 且不吞事件，
            // 让点击继续传给游戏。释放事件由 Godot 路由回接收按压的控件，因此拖出区域再松开也能正确收尾，无需校验释放点。
            if (!_visual.IsVisibleInTree() ||
                !new Rect2(Vector2.Zero, _inputBounds!.Size).HasPoint(buttonEvent.Position)) return;
            _pressStartedAt ??= Time.GetTicksMsec(); // 空赋值，只在首次按下时记录时间戳
        }
        else
        {
            if (_pressStartedAt == null && !_playingHold) return; // 没有按下也没有在播放长按时跳过
            var tapped = _pressStartedAt is { } start && Time.GetTicksMsec() - start < LongPressMilliseconds;
            CancelModelPress(); // 先统一收尾，避免重入播放
            if (tapped) PlayModelAnimation(TouchAnimation, loop: false); // 再播放一次“touch”动画
        }
        _inputBounds!.AcceptEvent(); // 接受事件，标记事件已经被处理，阻止继续传播。
    }
    
    // 统一收尾的状态清理逻辑
    private void CancelModelPress()
    {
        _pressStartedAt = null;
        if (!_playingHold) return; // 不是自己播放的长按动画就早返回
        _playingHold = false;
        if (_animationState?.GetCurrentAnimationName() != HoldAnimation) return; // 只处理长按动画的复原，避免影响其他动画。
        if (_creature != null) CreatureAnimation.RestoreIdleIfPlaying(_creature, "Idle"); // 战斗模型恢复
        else _animationState.SetAnimation(NormalAnimation, loop: true); // 预览模型恢复
    }
    
    public override void _Process(double delta)
    {
        if (_pressStartedAt is { } start && Time.GetTicksMsec() - start >= LongPressMilliseconds)
        {
            _pressStartedAt = null; // 清空按压记录，避免重入
            _playingHold = PlayModelAnimation(HoldAnimation, loop: true); // 循环播放，只有播放成功才持有恢复义务
        }
        // 读取真实 Spine 轨道的开始/结束信号，而不是逻辑触发名；自动排队回到 normal 也能被观察到。
        // 不在每帧创建 Spine 包装对象，且不会漏掉两帧间连续触发的非空闲动画。
        if (!_timer.Advance(delta, IndomitableConfiguration.IdleAnimationDelaySeconds,
                _visual.IsVisibleInTree() && (_creature?.Entity.IsAlive ?? true))) return;
        if (_animationState?.GetCurrentAnimationName() == NormalAnimation) // 只有当前是“normal”动画时才播放“motou”动画
            PlayModelAnimation(AnimationName, loop: false);
    }
    
    // 双模式动画播放
    private bool PlayModelAnimation(string name, bool loop)
    {
        if (_creature != null) return TryPlayCreatureAnimation(_creature, name, loop); // 战斗模式下委托给原版机制
        // 预览 Spine 模型分支和和战斗模型的分支的 TryPlayCreatureAnimation 一样，同名动画正在播放时直接跳过，不重启动画。
        if (_animationState == null || _animationState.GetCurrentAnimationName() == name || !_sprite.HasAnimation(name))
            return false;
        _animationState.SetAnimation(name, loop);
        if (!loop) _animationState.AddAnimation(NormalAnimation, loop: true); // 非循环播放时，添加下一个动作为“normal”循环。
        return true;
    }
    
    public override void _ExitTree()
    {
        CancelModelPress(); // 退出时也要清理按压处理的数据
        if (_inputBounds != null)
        {
            GetWindow().FocusExited -= CancelModelPress;
            if (IsInstanceValid(_inputBounds))
            {
                _inputBounds.GuiInput -= OnModelInput;
                _inputBounds.MouseFilter = _originalMouseFilter; // 恢复鼠标过滤
            }
        }
        if (IsInstanceValid(_visual)) _visual.VisibilityChanged -= OnVisibilityChanged;
        if (IsInstanceValid(_sprite.BoundObject))
        {
            _sprite.DisconnectAnimationStarted(_animationChanged);
            _sprite.DisconnectAnimationEnded(_animationChanged);
        }
        _animationChanged = default;
        // 保留所属模型及 Spine 引用，节点重新入树时可重新连接；切换皮肤则随父模型一起销毁。
        _animationState = null;
        _timer.ObserveAnimation(null);
    }
    
    /// <summary>只累计连续的 normal 时间；动画本身的播放时间不计入下一次等待。</summary>
    private sealed class IdleAnimationTimer
    {
        private bool _isIdle;
        private double _elapsed;
        
        public void ObserveAnimation(string? animationName)
        {
            _isIdle = animationName == NormalAnimation;
            if (!_isIdle) _elapsed = 0;
        }
        
        public bool Advance(double delta, int delaySeconds, bool canPlay)
        {
            if (!canPlay || !_isIdle || delaySeconds <= 0)
            {
                _elapsed = 0;
                return false;
            }
            // 演进 delta 的时长，超过阈值 delaySeconds 后清空计时并通知触发动画。
            _elapsed += delta;
            if (_elapsed < delaySeconds) return false;
            _elapsed = 0;
            return true;
        }
    }
}