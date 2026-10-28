using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

/// <summary>贝塞尔基础航线叠加法向物理运动；机头采用基础切线加 k*vy，不对扰动后的位置差分。</summary>
public sealed class AircraftFlightPath
{
    /// <summary>封装一段贝塞尔曲线，包括四个控制点和这段的结束点在总路程中的位置百分比。同时提供位置信息和切线斜率。</summary>
    private readonly record struct Leg(Vector2 A, Vector2 B, Vector2 C, Vector2 D, float End)
    {
        public Vector2 Position(float t) => A.BezierInterpolate(B, C, D, t); // 插值
        public Vector2 Tangent(float t) => A.BezierDerivative(B, C, D, t); // 微分
    }
    
    private readonly Leg[] _approach; // 接敌路径，俯冲轰炸机包括爬升和俯冲两段
    private readonly Leg _exit; // 撤离路径
    private readonly AircraftFlightSettings _settings;
    private readonly AircraftNormalMotion _normalMotion; // 法向物理抖动实例
    private readonly Vector2 _forward; // 基础水平朝向 (1,0) 或 (-1,0)
    public Vector2 BaseReleasePosition { get; } // 基础投弹位置
    public Vector2 ReleasePosition => Sample(1f); // 实际投弹位置，包含法向偏移，不强行将 y/速度归零。
    public float ApproachDuration { get; } // 接敌时间
    
    public AircraftFlightPath(Vector2 origin, Vector2 target, float viewportTop,
        AircraftFlightKind kind, AircraftFlightSettings settings, float phase)
    {
        _settings = settings;
        ApproachDuration = kind == AircraftFlightKind.DiveBomber
            ? settings.DiveApproachDuration : settings.ApproachDuration;
        // phase 作为本机随机种子输入；先确定 ζ/幅度界，离屏余量也要覆盖欠阻尼与补偿。
        _normalMotion = new AircraftNormalMotion(settings, BitConverter.SingleToInt32Bits(phase), ApproachDuration);
        var direction = target.X >= origin.X + settings.LaunchOffsetX ? 1f : -1f; // 初始位移正负性
        var forward = new Vector2(direction, 0f); // 飞机飞行方向
        _forward = forward;
        var start = origin + Vector2.Right * settings.LaunchOffsetX; // 起飞点偏移
        Vector2 releaseTangent; // 投弹瞬间的切线方向
        
        switch (kind)
        {
            case AircraftFlightKind.DiveBomber:
            {
                var angle = Mathf.DegToRad(settings.DiveAngleDegrees); // 俯冲角（弧度）
                releaseTangent = new Vector2(direction * Mathf.Cos(angle), Mathf.Sin(angle)); // 俯冲方向的单位向量
                BaseReleasePosition = target - releaseTangent * settings.DiveReleaseDistance; // 投弹位置坐标
                var high = new Vector2(target.X - direction * 160f, target.Y - settings.DiveHeight); // 俯冲的高点
                // 高点处水平过渡；最后两个控制点严格共线于目标，俯冲机头确实指向目标中心。
                _approach =
                [
                    new Leg(start, start + forward * 140f + Vector2.Up * 110f, high - forward * 110f, high, 0.55f),
                    new Leg(high, high + forward * 105f, BaseReleasePosition - releaseTangent * 135f, BaseReleasePosition, 1f)
                ];
                break;
            }
            case AircraftFlightKind.LevelBomber:
            {
                start.Y = target.Y - settings.LevelHeight; // 平飞高度
                BaseReleasePosition = new Vector2(target.X, start.Y); // 投弹位置位于敌人正上方
                var distance = Mathf.Abs(BaseReleasePosition.X - start.X); // 飞行距离
                releaseTangent = forward; // 水平投弹无俯冲
                _approach = [new Leg(start, start + forward * distance / 3f,
                    BaseReleasePosition - forward * distance / 3f, BaseReleasePosition, 1f)];
                break;
            }
            case AircraftFlightKind.StrikeFighter:
            case AircraftFlightKind.TorpedoBomber:
            default:
            {
                var height = kind == AircraftFlightKind.TorpedoBomber ? settings.TorpedoHeight : settings.FighterHeight;
                BaseReleasePosition = target + new Vector2(-direction * settings.ReleaseDistance,
                    kind == AircraftFlightKind.TorpedoBomber ? -height : -height * 0.4f);
                var cruise = new Vector2(Mathf.Lerp(start.X, BaseReleasePosition.X, 0.65f), target.Y - height);
                // 基于巡航点，鱼雷机水平投弹，战斗机带一点向下角度，模拟扫射/小角度俯冲
                releaseTangent = kind == AircraftFlightKind.TorpedoBomber
                    ? forward : new Vector2(direction, 0.35f).Normalized();
                var span = Mathf.Max(Mathf.Abs(BaseReleasePosition.X - start.X), 120f);
                _approach =
                [
                    new Leg(start, start + forward * span * 0.25f, cruise - forward * span * 0.2f, cruise, 0.65f),
                    new Leg(cruise, cruise + forward * span * 0.12f,
                        BaseReleasePosition - releaseTangent * span * 0.10f, BaseReleasePosition, 1f)
                ];
                break;
            }
        }
        
        // 先沿投放切线前进，再平滑拉起；法向位移上界也计入余量，确保有扰动时仍完整离屏。
        var end = new Vector2(BaseReleasePosition.X + direction * 270f,
            viewportTop - settings.ExitMargin - (float)_normalMotion.DisplacementBound);
        _exit = new Leg(BaseReleasePosition, BaseReleasePosition + releaseTangent * 85f,
            new Vector2(end.X - direction * 40f, end.Y + 180f), end, 1f);
    }
    
    /// <summary>最终位置采样</summary>
    public Vector2 Sample(float progress, bool exiting = false)
    {
        var tangent = Tangent(progress, exiting);
        // Godot 的 +Y 向下；沿切线的左手方向为 (ty, -tx)，向右飞时正偏移向上。
        var leftNormal = new Vector2(tangent.Y, -tangent.X);
        return BasePosition(progress, exiting) + leftNormal * (float)NormalState(progress, exiting).Displacement;
    }
    
    public Vector2 BasePosition(float progress, bool exiting = false)
    {
        var (leg, t) = Segment(progress, exiting);
        return leg.Position(t);
    }
    
    /// <summary>解析求取基础贝塞尔切线，不包含随机位移的一阶差分。</summary>
    public Vector2 Tangent(float progress, bool exiting = false)
    {
        var (leg, t) = Segment(progress, exiting);
        var tangent = leg.Tangent(t);
        return tangent.LengthSquared() > 0.000001f ? tangent.Normalized() : _forward; // 回归水平单位向量以防止除零错误。
    }
    
    /// <summary>法向运动采样，并从相对时间向绝对时间转换</summary>
    public AircraftNormalMotion.State NormalState(float progress, bool exiting = false) => _normalMotion.Sample(exiting
            ? ApproachDuration + (double)Mathf.Clamp(progress, 0f, 1f) * _settings.ExitDuration
            : (double)Mathf.Clamp(progress, 0f, 1f) * ApproachDuration);
    
    /// <summary>α = k*vy，左偏为正；Godot 屏幕坐标的旋转角与此符号相反。</summary>
    public Vector2 Heading(float progress, bool exiting = false) => Tangent(progress, exiting)
        .Rotated(-(float)(_settings.WobbleHeadingGain * NormalState(progress, exiting).Velocity));
    
    /// <summary>分段映射器。根据总进度的百分比返回对应的路径段和在该段中的进度百分比。</summary>
    private (Leg Leg, float Progress) Segment(float progress, bool exiting)
    {
        var t = Mathf.Clamp(progress, 0f, 1f);
        var leg = _exit;
        if (exiting) return (leg, t);
        var previous = 0f;
        foreach (var candidate in _approach)
        {
            if (t <= candidate.End)
            {
                leg = candidate;
                t = (t - previous) / (candidate.End - previous);
                break;
            }
            previous = candidate.End;
        }
        return (leg, t);
    }
}