using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

/// <summary>纯视觉节点：只使用本机随机种子，不消耗游戏 RNG；离树时必须释放等待攻击的任务。</summary>
public partial class AircraftFlightVfx : Node2D
{
    private readonly TaskCompletionSource _impact = new(); // 核心：控制异步等待，允许投弹后立即结算伤害而不必等待拉升结束
    private AircraftFlightPath _path = null!;
    private AircraftFlightSettings _settings = null!;
    private Sprite2D _plane = null!;
    private Vector2 _target;
    private AircraftFlightKind _kind;
    private float _elapsed; // 累计经过的时间（秒）
    
    /// <summary>等待弹药命中后继续原攻击；飞机拉升由节点独立播放，不延迟伤害结算。</summary>
    public static Task Play(CarrierAircraftCard card, IEnumerable<Creature> targets)
    {
        if (!IndomitableConfiguration.PlayAircraftFlightAnimations || NonInteractiveMode.IsActive
            || NCombatRoom.Instance is not { } room || card.AircraftModelPath == null
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant
            || card.Owner.Creature.GetCreatureNode() is not { } attackerNode
            || LoadTexture(card.AircraftModelPath) is not { } texture) return Task.CompletedTask;
        
        var container = room.CombatVfxContainer;
        // 筛选存活目标的视觉节点位置
        var positions = targets.Where(c => c.IsAlive)
            .Select(c => c.GetCreatureNode())
            .Where(n => n != null)
            .Select(n => n!.VfxSpawnPosition)
            .ToList();
        if (positions.Count == 0) return Task.CompletedTask;
        // 群体攻击选位置最接近左右边界中点的真实敌人；偶数敌人时稳定选择左侧。
        var middleX = (positions.Min(p => p.X) + positions.Max(p => p.X)) * 0.5f;
        var target = positions.OrderBy(p => Mathf.Abs(p.X - middleX)).ThenBy(p => p.X).First();
        // 坐标系转换：将世界坐标转换为 VFX 容器的局部坐标
        var toLocal = container.GetGlobalTransform().AffineInverse();
        var viewportToLocal = container.GetGlobalTransformWithCanvas().AffineInverse();
        var rect = container.GetViewportRect();
        // 视口顶部在局部坐标系下的 Y 值（考虑了可能的相机旋转/缩放）
        var top = Mathf.Min((viewportToLocal * rect.Position).Y,
            (viewportToLocal * new Vector2(rect.End.X, rect.Position.Y)).Y);
        var settings = card.AircraftFlightSettings;
        // 连同贴图边缘一起移出视口，不能只把精灵中心移出屏幕。
        var radius = texture.GetSize().Length() * Mathf.Max(settings.AttackScale, settings.ExitScale) * 0.5f;
        var vfx = new AircraftFlightVfx
        {
            _settings = settings,
            _kind = card.AircraftFlightKind,
            _target = toLocal * target,
            _plane = new Sprite2D { Texture = texture },
            ZIndex = 20     // 确保飞机渲染在大多数战斗特效之上
        };
        vfx._path = new AircraftFlightPath(toLocal * attackerNode.VfxSpawnPosition, vfx._target,
            top - radius, vfx._kind, settings, Random.Shared.NextSingle() * Mathf.Tau);
        vfx.AddChild(vfx._plane);
        container.AddChild(vfx);
        vfx.UpdatePlane();
        return vfx._impact.Task;
    }
    
    private static Texture2D? LoadTexture(string? path)
    {
        if (path == null) return null;
        var tres = Path.ChangeExtension(path, ".tres"); // .tres 优先
        if (ResourceLoader.Exists(tres, "Texture2D")) return ResourceLoader.Load<Texture2D>(tres);
        return ResourceLoader.Exists(path, "Texture2D") ? ResourceLoader.Load<Texture2D>(path) : null;
    }
    
    public override void _Process(double delta)
    {
        // 最后一击结束战斗后，已命中的飞机仍可完成拉升；场景离树时仍自动清理。
        // 命中前结束战斗则立即释放等待，避免已取消的攻击挂起。
        if ((CombatManager.Instance.IsOverOrEnding && !_impact.Task.IsCompleted)
            || !IndomitableConfiguration.PlayAircraftFlightAnimations
            || SaveManager.Instance.PrefsSave.FastMode == FastModeType.Instant)
        {
            Finish();
            return;
        }
        // 不调用 CustomScaledWait，避免快速模式对同一段动画重复加速。
        _elapsed += (float)delta * (SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 2f : 1f);
        UpdatePlane();
        QueueRedraw();
        if (_elapsed >= _path.ApproachDuration + Mathf.Max(_settings.ExitDuration, _settings.OrdnanceDuration))
            Finish();
        else if (_elapsed >= _path.ApproachDuration + _settings.OrdnanceDuration)
            // 放在本帧最后：继续攻击可能结束战斗并销毁场景，不再访问任何原生节点。
            _impact.TrySetResult(); // 通知游戏逻辑开始计算伤害。而 VFX 节点自己会继续执行
    }
    
    private void UpdatePlane()
    {
        var exitTime = _elapsed - _path.ApproachDuration;
        var exiting = exitTime >= 0f;
        var progress = Mathf.Clamp(exiting
            ? exitTime / Mathf.Max(_settings.ExitDuration, 0.001f)
            : _elapsed / Mathf.Max(_path.ApproachDuration, 0.001f), 0f, 1f);
        _plane.Position = _path.Sample(progress, exiting);
        // 左向原图先镜像绘制；机头使用基础切线加小幅 k*vy 偏转，避免位置微扰造成大角度抖动。
        // FlipH 仅改变 Sprite2D 的绘制，不改变节点旋转，因此补偿角也要按镜像后的机头计算。
        _plane.FlipH = _settings.TextureFlipH;
        var textureForward = Mathf.DegToRad(_settings.TextureForwardAngleDegrees);
        if (_settings.TextureFlipH) textureForward = Mathf.Pi - textureForward;
        _plane.Rotation = _path.Heading(progress, exiting).Angle() - textureForward;
        // 缩放计算。缩放进度采用 0~1 的相对值；拉升保留短暂停顿，并用平滑阶跃函数缓慢缩小；接敌仍使用立方缓出函数快速放大；最终插值计算。
        var scaleProgress = Mathf.Clamp(exiting
            ? (exitTime - _settings.ExitShrinkDelay) / Mathf.Max(_settings.ShrinkDuration, 0.001f)
            : _elapsed / Mathf.Max(_settings.GrowDuration, 0.001f), 0f, 1f);
        var ease = exiting ? scaleProgress * scaleProgress * (3f - 2f * scaleProgress)
            : 1f - Mathf.Pow(1f - scaleProgress, 3f);
        _plane.Scale = Vector2.One * Mathf.Lerp(exiting ? _settings.AttackScale : _settings.StartScale,
            exiting ? _settings.ExitScale : _settings.AttackScale, ease);
    }
    
    public override void _Draw()
    {
        var fall = (_elapsed - _path.ApproachDuration) / Mathf.Max(_settings.OrdnanceDuration, 0.001f);
        if (fall is < 0f or >= 1f) return;
        var from = _path.ReleasePosition;
        // 鱼雷贴着低空向前滑行；炸弹加速下落。无额外音效，以免覆盖卡牌原有命中音效。
        var position = from.Lerp(_target, _kind == AircraftFlightKind.TorpedoBomber ? fall : fall * fall);
        var direction = (_target - from).Normalized();
        DrawLine(position - direction * 7f, position + direction * 4f, new Color(0.27f, 0.30f, 0.25f), 4f, true);
        DrawCircle(position + direction * 4f, 2f, new Color(0.78f, 0.66f, 0.37f));
    }
    
    private void Finish()
    {
        SetProcess(false);
        QueueFree();
        _impact.TrySetResult();
    }
    
    public override void _ExitTree() => _impact.TrySetResult();
}