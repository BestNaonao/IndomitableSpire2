namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

/// <summary>
/// 法向运动：y'' + 2 ζω y' + ω² y = a0 G(ζ) ξ(t)，ω² = a0 / yt。
/// 默认每架抽取 ζ∈[0.6,1.2]；增益 G 轻微补偿高阻尼的幅度，固有频率保持不变。
/// 固定 120 Hz 预积分；每个小步按常力精确求解，查询也精确推进剩余小步，不依赖渲染帧率/调用顺序。
/// </summary>
public sealed class AircraftNormalMotion
{
    public readonly record struct State(double Displacement, double Velocity);
    
    private const double Step = 1d / 120d;          // 模拟积分步长，即 dt
    private readonly State[] _states = [default];   // 预计算的飞行状态数组，即 (y, y')
    private readonly double[] _forces = [];         // 飞行过程中的驱动力数组，即 a0 G(ζ) ξ(t)
    private readonly double _omega;                 // 系统固有角频率 ω = sqrt(a0 / yt)
    private readonly double _dampingRatio;          // 阻尼比 ζ
    
    public double DampingRatio => _dampingRatio;
    public double NaturalFrequency => _omega;
    public double AmplitudeGain { get; } = 1d;      // 振幅增益比例
    public double DisplacementBound { get; }        // 位移界线
    public double VelocityBound { get; }            // 速度界线
    
    public AircraftNormalMotion(AircraftFlightSettings settings, int seed, double approachDuration)
    {
        if (settings.WobbleAmplitude <= 0f || settings.WobbleAcceleration <= 0f) return;
        _omega = Math.Sqrt(settings.WobbleAcceleration / (double)settings.WobbleAmplitude);
        var random = new Random(seed); // 只使用本机视觉种子，不接触战斗 RNG。
        _dampingRatio = Math.Max(0.01d, settings.WobbleDampingRatio)
            + Math.Max(0d, settings.WobbleDampingRandomness) * random.NextDouble();
        // 带补偿项的振幅增益比例
        AmplitudeGain = 1d + Math.Max(0d, settings.WobbleDampingCompensation)
            * Math.Max(0d, _dampingRatio - settings.WobbleCompensationReference);
        // 只放大驱动，保持 a0/yt 的回正系数和固有频率；正负方向使用相同增益。
        var acceleration = settings.WobbleAcceleration * AmplitudeGain;
        if (_dampingRatio < 1d)
        {
            var beta = Math.Sqrt(1d - _dampingRatio * _dampingRatio);
            var q = Math.Exp(-Math.PI * _dampingRatio / beta);
            DisplacementBound = settings.WobbleAmplitude * AmplitudeGain * (1d + q) / (1d - q);
            VelocityBound = 2d * acceleration / _omega
                * Math.Exp(-_dampingRatio * Math.Acos(_dampingRatio) / beta) / (1d - q);
        }
        else
        {
            DisplacementBound = settings.WobbleAmplitude * AmplitudeGain;
            // 临界阻尼的脉冲峰值；过阻尼的峰值更低，取同一保守速度界。
            VelocityBound = 2d * acceleration / (Math.E * _omega);
        }
        var duration = approachDuration + Math.Max(0d, settings.ExitDuration);
        var count = (int)Math.Ceiling(duration / Step); // 总计算步骤数
        _states = new State[count + 1]; // 状态数组
        _forces = new double[count];    // 驱动力数组
        var interval = Math.Max(Step, settings.WobbleNoiseInterval);    // 平滑插值间隔，至少为一个步长
        var noise = new double[(int)Math.Ceiling(count * Step / interval) + 2]; // 噪声数组，每节一个，节间插值
        for (var i = 0; i < noise.Length; i++) noise[i] = random.NextDouble() * 2d - 1d;
        for (var i = 0; i < count; i++)
        {
            var time = (i + 0.5d) * Step;   // 时间点
            var knot = (int)(time / interval);  // 节数
            var t = time / interval - knot; // 相对节数，t ∈ [0, 1]。
            t *= t * (3d - 2d * t); // 平滑连接噪声控制点；输入始终位于 [-1, 1]。
            var input = noise[knot] + (noise[knot + 1] - noise[knot]) * t;  // 各个噪点之间的插值
            var fade = settings.WobbleFadeDuration > 0f
                ? Math.Clamp((time - approachDuration + settings.WobbleFadeDuration) / settings.WobbleFadeDuration, 0d, 1d)
                : time >= approachDuration ? 1d : 0d;
            fade *= fade * (3d - 2d * fade);
            // 结合 ExitWobbleStrength，控制撤离阶段的抖动强度
            var strength = 1d + (Math.Clamp(settings.ExitWobbleStrength, 0f, 1f) - 1d) * fade;
            _forces[i] = acceleration * input * strength;
            _states[i + 1] = Advance(_states[i], _forces[i], Step);
        }
    }
    
    /// <summary>
    /// 外部取样查询接口。找到给定时间对应的预计算索引，然后对剩余的非整数步长时间再次调用 Advance 进行精确插值，保证任意帧率下采样结果绝对一致。
    /// </summary>
    /// <param name="time">任意时间 time</param>
    /// <returns>时间点状态</returns>
    public State Sample(double time)
    {
        if (_forces.Length == 0 || time <= 0d) return default;
        if (time >= _forces.Length * Step) return _states[^1];
        var index = (int)(time / Step);
        return Advance(_states[index], _forces[index], time - index * Step);
    }
    
    /// <summary>
    /// 阻尼谐振子受迫振动微分方程的解析求解器。已知某一时刻的位移和速度，以及当前受到的恒定外力，精确计算出经过一定时间后，系统的新位移和新速度。
    /// </summary>
    /// <param name="state">当前位移、速度状态</param>
    /// <param name="force">外部驱动力</param>
    /// <param name="time">演算时间</param>
    /// <returns></returns>
    private State Advance(State state, double force, double time)
    {
        var equilibrium = force / (_omega * _omega); // 平衡点 y_eq = F / ω²。
        var displacement = state.Displacement - equilibrium; // 转换为相对于平衡位置的齐次方程求解。设 x = y - eq。
        // 临界阻尼分支，齐次方程 x'' + 2 ζω x' + ω² x = 0 的通解为 x = (C_1 + C_2 t) * exp(-ω t)。
        if (Math.Abs(_dampingRatio - 1d) < 1e-8d)
        {
            var c = state.Velocity + _omega * displacement; // 由初始条件得 C_1 = x|0, C_2 = y'|0 + ω C_1。
            var decay = Math.Exp(-_omega * time); // 公共衰减因子
            return new State(equilibrium + (displacement + c * time) * decay,
                (state.Velocity - _omega * c * time) * decay);
        }
        // 欠阻尼分支，通解为 x = exp(- ζ ω t)(A cos(ω_d t) + B sin(ω_d t))，其中阻尼固有频率 ω_d = ω sqrt(1 - ζ²)。
        if (_dampingRatio < 1d)
        {
            var dampedFrequency = _omega * Math.Sqrt(1d - _dampingRatio * _dampingRatio);
            var decay = Math.Exp(-_dampingRatio * _omega * time);
            var sine = Math.Sin(dampedFrequency * time) / dampedFrequency;
            var cosine = Math.Cos(dampedFrequency * time);
            return new State(equilibrium + decay * (displacement * cosine
                    + (state.Velocity + _dampingRatio * _omega * displacement) * sine),
                decay * (state.Velocity * cosine
                    - (_dampingRatio * _omega * state.Velocity + _omega * _omega * displacement) * sine));
        }
        // 过阻尼分支，通解为 x = A exp(r_1 t) + B exp(r_2 t)，r_1,2 = - ζ ω ± ω sqrt(ζ² - 1)。
        var sum = _dampingRatio + Math.Sqrt(_dampingRatio * _dampingRatio - 1d);
        var slow = -_omega / sum;
        var fast = -_omega * sum;
        var a = (state.Velocity - fast * displacement) / (slow - fast);
        var b = displacement - a; // x|0 = A + B, y'|0 = A r_1 + B r_2。
        a *= Math.Exp(slow * time);
        b *= Math.Exp(fast * time);
        return new State(equilibrium + a + b, slow * a + fast * b);
    }
}