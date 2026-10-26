namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

public enum AircraftFlightKind { StrikeFighter, TorpedoBomber, DiveBomber, LevelBomber }

/// <summary>
/// 舰载机演出调参入口。距离以战斗画布像素计，时间以普通速度的秒计，缩放是原贴图倍率。
/// 可在 CarrierAircraftCard.AircraftFlightSettings 中用 Default with { ... } 为单张牌调整。
/// </summary>
public sealed record AircraftFlightSettings
{
    public static AircraftFlightSettings Default { get; } = new();
    
    public float LaunchOffsetX { get; init; } = 90f; // 人物中心右侧的起飞距离。
    public float StartScale { get; init; } = 0.15f; // 起飞时缩放。
    public float AttackScale { get; init; } = 0.60f; // 接近目标时的最大缩放。
    public float ExitScale { get; init; } = 0.20f; // 离开屏幕时缩放，避免拉升时过早小到无法辨认。
    public float GrowDuration { get; init; } = 0.18f; // 起飞后快速放大的时间。
    public float ShrinkDuration { get; init; } = 0.95f; // 拉升缩小时间，使用两端平缓的 smoothstep。
    public float ExitShrinkDelay { get; init; } = 0.16f; // 投弹后保持接敌大小的时间，不延迟命中结算。
    public float ApproachDuration { get; init; } = 0.72f; // 三种普通机种共用的接敌时间。
    public float DiveApproachDuration { get; init; } = 1.02f; // 俯冲机额外包含爬高时间。
    public float ExitDuration { get; init; } = 1.25f; // 投弹后拉升出屏时间；与命中特效/伤害并行。
    public float OrdnanceDuration { get; init; } = 0.12f; // 弹药从释放到命中的时间。
    public float FighterHeight { get; init; } = 125f; // 相对目标中心的中空高度。
    public float TorpedoHeight { get; init; } = 12f; // 相对目标中心的低空高度。
    public float LevelHeight { get; init; } = 225f; // 水平机从起飞起就处于此高度。
    public float DiveHeight { get; init; } = 345f; // 俯冲机最高点，比水平机更高。
    public float ReleaseDistance { get; init; } = 95f; // 战斗机/鱼雷机在目标前方的投放距离。
    public float DiveReleaseDistance { get; init; } = 120f; // 俯冲释放点到目标中心的距离。
    public float DiveAngleDegrees { get; init; } = 68f; // 向下俯冲角，释放瞬间切线指向目标中心。
    public float ExitMargin { get; init; } = 180f; // 超出视口上边界的距离，另加贴图半径。
    public float WobbleAmplitude { get; init; } = 4f; // yt，基本回正尺度（px）；实际位移上界还取决于 ζ/补偿，0 禁用。
    public float WobbleAcceleration { get; init; } = 160f; // 基本 a0（px/s²）；ω²=a0/yt，默认固有周期约 0.99 秒。
    public float WobbleDampingRatio { get; init; } = 0.6f; // ζ 下限，须大于 0；每架抽取一次，飞行中保持不变。
    public float WobbleDampingRandomness { get; init; } = 0.6f; // ζ 的随机宽度；默认范围为 [0.6,1.2]。
    public float WobbleCompensationReference { get; init; } = 0.6f; // 幅度补偿参考阻尼比，ζ 不超过此值时增益为 1。
    public float WobbleDampingCompensation { get; init; } = 0.25f; // G=1+此值*max(0,ζ-参考值)；ζ=1.2 时增加约 15%。
    public float WobbleNoiseInterval { get; init; } = 0.18f; // 随机加速度控制点间隔（秒），点间平滑插值。
    public float WobbleHeadingGain { get; init; } = 0.003f; // k，α=k*vy（rad）；单位 rad·s/px，默认阻尼范围内偏转上界约 4.8°。
    public float WobbleFadeDuration { get; init; } = 0.16f; // 投弹前将随机输入平滑过渡到拉升强度的时间。
    public float ExitWobbleStrength { get; init; } = 0f; // 拉升随机输入比例 [0,1]；0 仍连续保留 y、vy 并阻尼回正。
    public bool TextureFlipH { get; init; } = true; // 左向素材先水平镜像，使驾驶舱保持朝上；右向素材可设 false。
    public float TextureForwardAngleDegrees { get; init; } = 180f; // 镜像前原图的机头角度；朝左 180，朝右 0。
}