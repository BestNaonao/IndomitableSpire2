using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Animation;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>随角色节点创建和销毁的本地空闲演出，不参与战斗状态或网络同步。</summary>
public partial class IndomitableIdleAnimation : Node
{
    private const string NodeName = "IndomitableIdleAnimation";
    private const string AnimationName = "motou";
    private readonly IdleAnimationTimer _timer = new();
    private NCreature _creature = null!;
    private MegaSprite? _sprite;
    private MegaAnimationState? _animationState;
    private Callable _animationChanged;
    
    internal static void Attach(NCreature creature)
    {
        if (creature.Entity.Player?.Character is not Indomitable || CreatureAnimation.GetAnimator(creature) == null ||
            creature.Visuals.SpineBody is not { } sprite || !sprite.HasAnimation(AnimationName) ||
            creature.GetNodeOrNull<IndomitableIdleAnimation>(NodeName) != null) return;
        creature.AddChild(new IndomitableIdleAnimation { Name = NodeName, _creature = creature });
    }
    
    internal static bool TryPlay(NCreature creature)
    {
        // 按实际 Spine 动作判断，不区分定时器或 Ping 来源；重复 Ping 只显示气泡，不重启动画。
        if (creature.Entity is not { IsAlive: true, Player.Character: Indomitable } || !CreatureAnimation
                .TryResolveSpineAnimation(creature, AnimationName, out var trigger, animationGroup: "Idle") ||
            creature.SpineAnimation.GetCurrentAnimationName() == AnimationName) return false;
        // 使用现有一次性动画状态，播放完自动回到原有 Idle；不触发卡牌音效或等待。
        creature.SetAnimationTrigger(trigger);
        return true;
    }
    
    public override void _EnterTree()
    {
        _sprite = _creature.Visuals.SpineBody;
        _animationState = _sprite!.GetAnimationState();
        _animationChanged = Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationChanged);
        _sprite.ConnectAnimationStarted(_animationChanged);
        _sprite.ConnectAnimationEnded(_animationChanged);
        ObserveCurrentAnimation();
    }
    
    private void OnAnimationChanged(GodotObject _, GodotObject __, GodotObject ___) => ObserveCurrentAnimation();
    
    private void ObserveCurrentAnimation() => _timer.ObserveAnimation(_animationState?.GetCurrentAnimationName());
    
    public override void _Process(double delta)
    {
        // 读取真实 Spine 轨道的开始/结束信号，而不是逻辑触发名；自动排队回到 normal 也能被观察到。
        // 不在每帧创建 Spine 包装对象，且不会漏掉两帧间连续触发的非空闲动画。
        if (_timer.Advance(delta, IndomitableConfiguration.IdleAnimationDelaySeconds,
                _creature.Entity.IsAlive && _creature.IsVisibleInTree()))
            TryPlay(_creature);
    }
    
    public override void _ExitTree()
    {
        if (_sprite != null && IsInstanceValid(_sprite.BoundObject))
        {
            _sprite.DisconnectAnimationStarted(_animationChanged);
            _sprite.DisconnectAnimationEnded(_animationChanged);
        }
        _animationChanged = default;
        _sprite = null;
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
            _isIdle = animationName == "normal";
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