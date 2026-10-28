"""舰载机法向位移调参图（Python 3.10+，依赖 matplotlib）。

安装：python -m pip install -r requirements_aircraft_motion.txt
运行：python aircraft_normal_motion_plot.py
示例：python aircraft_normal_motion_plot.py --zeta-min 0.2 --zeta-max 1.5 --seed 42
      python aircraft_normal_motion_plot.py --a0 160 --yt 5 --duration-ms 5000

默认读取 ../Animation/AircraftFlightSettings.cs 的当前数值初值，也可在下面覆盖或用命令行调整。
默认打开单个交互图窗：滑块以 0.01 调节阻尼比；按钮或 R 键重新生成 ξ(t)。
--sweep 保留每隔 0.05 逐图展示模式，关闭当前图窗后显示下一个。
通过 Matplotlib 工具栏手动保存；Q/Esc 结束整次浏览。脚本不自动保存图片。

与 C# 一致：零初态、120 Hz 常力小步解析积分、smoothstep 噪声插值、投弹前强度衰减。
默认范围、固有频率和高阻尼幅度补偿均读取 C#；预览固定当前 ζ，不叠加阻尼随机量。
调节 ζ 时保持同一组噪声控制点，重新生成按钮才更换随机种子。
Python 随机数序列不要求与 .NET Random 逐点一致。

注意：欠阻尼时，ξ 恒取 +1/-1 的曲线并非所有随机轨迹的严格上下界。
灰色阴影额外给出当前离散算法、控制点均在 [-1,1] 时的逐时包络。
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, replace
from decimal import Decimal
import math
from pathlib import Path
import random
import re
import sys


# ---------- 手动调参入口 ----------
ZETA_MIN = None  # None 读取 C# 的范围下限。
ZETA_MAX = None  # None 读取 C# 的下限 + 随机宽度。
INITIAL_ZETA = None  # None 使用当前范围中点。
SEED = 42  # 固定种子方便多次调参对比；修改此值可换一条随机轨迹。
DURATION_MS = None  # None 使用当前接敌时长 + 拉升时长；可加长以观察欠阻尼尾部。
COMMON_Y_SCALE = True  # 所有图使用相同的纵轴范围。

# None 表示读取 C#。这里的时间单位与 C# 一样是秒，图上的横坐标统一换算为 ms。
PARAMETER_OVERRIDES = {
    "WobbleAcceleration": None,     # a0，px/s²。
    "WobbleAmplitude": None,        # yt，px；0 关闭扰动。
    "WobbleDampingRatio": None,
    "WobbleDampingRandomness": None,
    "WobbleCompensationReference": None,
    "WobbleDampingCompensation": None,
    "WobbleNoiseInterval": None,
    "WobbleFadeDuration": None,
    "ExitWobbleStrength": None,
    "ApproachDuration": None,
    "DiveApproachDuration": None,
    "ExitDuration": None,
}
STEP = 1.0 / 120.0
ZETA_STEP = Decimal("0.05")
SETTINGS_PATH = Path(__file__).resolve().parents[1] / "Animation" / "AircraftFlightSettings.cs"


@dataclass(frozen=True)
class MotionConfig:
    a0: float
    yt: float
    noise_interval: float
    fade_duration: float
    exit_strength: float
    approach_duration: float
    duration: float
    seed: int
    damping_reference: float = 0.6
    damping_compensation: float = 0.25


@dataclass
class Curves:
    zeta: float
    times_ms: list[float]
    displacement: list[float]
    maximum_input: list[float]
    minimum_input: list[float]
    envelope: list[float]
    gain: float = 1.0


def read_game_parameters(path: Path = SETTINGS_PATH) -> dict[str, float]:
    source = path.read_text(encoding="utf-8-sig")
    values = {}
    for name, override in PARAMETER_OVERRIDES.items():
        if override is not None:
            values[name] = float(override)
            continue
        match = re.search(
            rf"\b{re.escape(name)}\s*\{{\s*get;\s*init;\s*\}}\s*=\s*"
            r"([-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?)\s*[fFdD]?\s*;",
            source,
        )
        if match is None:
            raise ValueError(f"无法读取 {name} 的数值初值：{path}")
        values[name] = float(match.group(1))
    return values


def damping_values(lower: float, upper: float) -> list[float]:
    if not math.isfinite(lower) or not math.isfinite(upper) or lower < 0 or upper < lower:
        raise ValueError("阻尼比范围必须满足 0 <= 下限 <= 上限，且均为有限数。")
    start, stop = Decimal(str(lower)), Decimal(str(upper))
    count = (stop - start) / ZETA_STEP
    if count != count.to_integral_value():
        raise ValueError("阻尼比上下限之差必须是 0.05 的整数倍，以便包含两个端点。")
    return [float(start + i * ZETA_STEP) for i in range(int(count) + 1)]


def smoothstep(t: float) -> float:
    t = min(1.0, max(0.0, t))
    return t * t * (3.0 - 2.0 * t)


def amplitude_gain(config: MotionConfig, zeta: float) -> float:
    """放大驱动而不改变 a0/yt，补偿高阻尼幅度并保留同一固有频率。"""
    return 1.0 + max(0.0, config.damping_compensation) * max(0.0, zeta - config.damping_reference)


def transition(omega: float, zeta: float, dt: float) -> tuple[float, ...]:
    """与 C# 一致的欠阻尼、临界阻尼、过阻尼精确常力状态转移。"""
    omega2 = omega * omega
    if abs(zeta - 1.0) < 1e-8:
        decay = math.exp(-omega * dt)
        m00, m01 = decay * (1.0 + omega * dt), decay * dt
        m10, m11 = -omega2 * m01, decay * (1.0 - omega * dt)
    elif zeta < 1.0:
        damped = omega * math.sqrt(1.0 - zeta * zeta)
        decay = math.exp(-zeta * omega * dt)
        sine = math.sin(damped * dt) / damped
        cosine = math.cos(damped * dt)
        m00 = decay * (cosine + zeta * omega * sine)
        m01 = decay * sine
        m10 = -omega2 * m01
        m11 = decay * (cosine - zeta * omega * sine)
    else:
        root_sum = zeta + math.sqrt(zeta * zeta - 1.0)
        slow, fast = -omega / root_sum, -omega * root_sum
        denominator = slow - fast
        slow_decay, fast_decay = math.exp(slow * dt), math.exp(fast * dt)
        m00 = (-fast * slow_decay + slow * fast_decay) / denominator
        m01 = (slow_decay - fast_decay) / denominator
        m10 = -omega2 * m01
        m11 = (slow * slow_decay - fast * fast_decay) / denominator
    return m00, m01, m10, m11, (1.0 - m00) / omega2, -m10 / omega2


def simulate(config: MotionConfig, zeta: float) -> Curves:
    count = math.ceil(config.duration / STEP)
    times = [min(i * STEP, config.duration) for i in range(count + 1)]
    gain = amplitude_gain(config, zeta)
    result = Curves(zeta, [t * 1000.0 for t in times], [0.0], [0.0], [0.0], [0.0], gain)
    if config.a0 == 0 or config.yt == 0:
        zeros = [0.0] * len(times)
        return Curves(zeta, result.times_ms, zeros, zeros.copy(), zeros.copy(), zeros.copy(), gain)

    omega = math.sqrt(config.a0 / config.yt)
    interval = max(STEP, config.noise_interval)
    knot_count = math.ceil(count * STEP / interval) + 2
    rng = random.Random(config.seed)
    noise = [rng.uniform(-1.0, 1.0) for _ in range(knot_count)]
    # y(t) = Σ c_j(t)*ξ_j。保留每个噪声控制点的线性响应，既能还原随机轨迹也能求严格包络。
    y_coefficients = [0.0] * knot_count
    v_coefficients = [0.0] * knot_count
    full_step = transition(omega, zeta, STEP)
    for i in range(count):
        midpoint = (i + 0.5) * STEP  # 与 C# 一样，最后一个不完整小步也沿用完整小步的中点受力。
        knot = int(midpoint / interval)
        weight = smoothstep(midpoint / interval - knot)
        fade = smoothstep((midpoint - config.approach_duration + config.fade_duration) / config.fade_duration) \
            if config.fade_duration > 0 else float(midpoint >= config.approach_duration)
        strength = 1.0 + (config.exit_strength - 1.0) * fade
        force = config.a0 * gain * strength
        dt = times[i + 1] - times[i]
        m00, m01, m10, m11, by, bv = full_step if abs(dt - STEP) < 1e-12 else transition(omega, zeta, dt)
        next_y = [m00 * y + m01 * v for y, v in zip(y_coefficients, v_coefficients)]
        next_v = [m10 * y + m11 * v for y, v in zip(y_coefficients, v_coefficients)]
        for j, contribution in ((knot, force * (1.0 - weight)), (knot + 1, force * weight)):
            next_y[j] += by * contribution
            next_v[j] += bv * contribution
        y_coefficients, v_coefficients = next_y, next_v
        result.displacement.append(math.fsum(c * xi for c, xi in zip(next_y, noise)))
        maximum = math.fsum(next_y)  # 所有 ξ_j=+1 即 ξ(t) 始终取最大值。
        result.maximum_input.append(maximum)
        result.minimum_input.append(-maximum)
        # 每个控制点 ξ_j∈[-1,1]：max |Σ c_j ξ_j| = Σ |c_j|。
        # 这是逐时包络：不同时间的边界通常由不同的噪声序列实现。
        result.envelope.append(math.fsum(abs(c) for c in next_y))
    return result


def draw_curves(ax, curves: Curves, config: MotionConfig, number: int, total: int, y_limit: float | None):
    ax.clear()
    kind = "无阻尼" if curves.zeta == 0 else "临界阻尼" if abs(curves.zeta - 1) < 1e-8 else "欠阻尼" if curves.zeta < 1 else "过阻尼"
    ax.fill_between(curves.times_ms, [-b for b in curves.envelope], curves.envelope,
                    color="#94a3b8", alpha=0.22, label="有界噪声的逐时包络")
    ax.plot(curves.times_ms, curves.maximum_input, "--", color="#d64b40", linewidth=1.6,
            label="ξ(t)≡+1：恒定最大驱动")
    ax.plot(curves.times_ms, curves.minimum_input, "--", color="#4277bb", linewidth=1.6,
            label="ξ(t)≡−1：恒定最小驱动")
    ax.plot(curves.times_ms, curves.displacement, color="#0f766e", linewidth=2.0,
            label=f"随机轨迹（seed={config.seed}）")
    release = config.approach_duration * 1000.0
    if 0 < release < curves.times_ms[-1]:
        ax.axvline(release, color="#b7791f", linestyle=":", linewidth=1.2, label="投弹 / 拉升开始")
    ax.axhline(0, color="#64748b", linewidth=0.7)
    ax.set(xlabel="时间 (ms)", ylabel="法向位移 y (px)", xlim=(0, curves.times_ms[-1]))
    omega = math.sqrt(config.a0 / config.yt) if config.a0 > 0 and config.yt > 0 else 0
    period = f"{math.tau / omega * 1000:.0f}" if omega > 0 else "n/a"
    ax.set_title(f"ζ={curves.zeta:.2f}（{kind}） | {number}/{total}\n"
                 f"ω0={omega:.3f} rad/s，T0={period} ms，补偿 G={curves.gain:.3f}，seed={config.seed}\n"
                 f"a0={config.a0:g} px/s²，yt={config.yt:g} px，噪声间隔={config.noise_interval * 1000:g} ms，"
                 f"拉升驱动比例={config.exit_strength:g}")
    if y_limit is not None:
        ax.set_ylim(-y_limit, y_limit)
    ax.grid(True, alpha=0.2)
    ax.legend(loc="best", fontsize=9)


def make_figure(curves: Curves, config: MotionConfig, number: int, total: int, y_limit: float | None):
    """无控件的静态图构造入口，供数值/渲染检查使用。"""
    import matplotlib.pyplot as plt

    fig, ax = plt.subplots(figsize=(11, 6), layout="constrained")
    fig.canvas.manager.set_window_title(f"舰载机法向位移 {number}/{total} | ζ={curves.zeta:.2f}")
    draw_curves(ax, curves, config, number, total, y_limit)
    fig.supxlabel("工具栏手动保存图片；关闭窗口显示下一张；Q / Esc 结束浏览", fontsize=10)
    return fig


class PreviewWindow:
    """控件持有强引用；调节 ζ 保持噪声，重新生成时只更换噪声种子。"""

    def __init__(self, config: MotionConfig, zeta: float, lower: float, upper: float,
                 number: int, total: int, y_limit: float | None):
        import matplotlib.pyplot as plt
        from matplotlib.widgets import Button, Slider

        self.config, self.number, self.total, self.y_limit = config, number, total, y_limit
        self.stop_requested = False
        self.figure, self.axes = plt.subplots(figsize=(11, 7))
        self.figure.subplots_adjust(left=0.08, right=0.98, bottom=0.27, top=0.81)
        self.slider = None
        slider_axes = self.figure.add_axes((0.18, 0.135, 0.48, 0.035))
        if upper > lower:
            self.slider = Slider(slider_axes, "阻尼比 ζ", lower, upper,
                                 valinit=zeta, valstep=0.01, valfmt="%.2f")
            self.slider.on_changed(self.redraw)
            zeta = float(self.slider.val)
        else:
            slider_axes.set_axis_off()
            slider_axes.text(0, 0.5, f"固定阻尼比 ζ={zeta:.2f}")
        button_axes = self.figure.add_axes((0.75, 0.11, 0.21, 0.075))
        self.regenerate_button = Button(button_axes, "重新生成 ξ(t) [R]")
        self.regenerate_button.on_clicked(self.regenerate)
        self.figure.canvas.mpl_connect("key_press_event", self.on_key)
        self.figure.text(0.5, 0.025, "滑块调节 ζ；R/按钮重新生成噪声；Q/Esc 结束；工具栏手动保存",
                         ha="center", fontsize=10)
        self.redraw(zeta)

    def redraw(self, zeta):
        self.current = simulate(self.config, round(float(zeta), 6))
        if self.y_limit is not None:
            self.y_limit = max(self.y_limit, max(self.current.envelope) * 1.08)
        draw_curves(self.axes, self.current, self.config, self.number, self.total, self.y_limit)
        self.figure.canvas.manager.set_window_title(
            f"舰载机法向位移 | ζ={self.current.zeta:.2f} | seed={self.config.seed}")
        self.figure.canvas.draw_idle()

    def regenerate(self, _event=None):
        seed = random.SystemRandom().randrange(2**31)
        if seed == self.config.seed:
            seed = (seed + 1) % (2**31)
        self.config = replace(self.config, seed=seed)
        self.redraw(self.current.zeta)
        print(f"重新生成噪声：ζ={self.current.zeta:.2f}，seed={seed}")

    def on_key(self, event):
        import matplotlib.pyplot as plt

        if event.key in {"r", "R"}:
            self.regenerate()
        elif event.key in {"q", "escape"}:
            self.stop_requested = True
            plt.close(self.figure)


def show_curves(curves: list[Curves], config: MotionConfig, common_y_scale: bool,
                *, sweep: bool = False, initial_zeta: float | None = None) -> None:
    try:
        import matplotlib
        import matplotlib.font_manager as fonts
        import matplotlib.pyplot as plt
    except ModuleNotFoundError as error:
        raise SystemExit("缺少绘图依赖。请运行：python -m pip install matplotlib") from error

    if matplotlib.get_backend().lower() in {"agg", "pdf", "svg", "ps", "pgf", "cairo", "template"}:
        raise SystemExit("当前 Matplotlib 后端无法弹出图窗。请在带 Tk/Qt 的本机 Python 中运行，并取消 MPLBACKEND=Agg。")
    available = {font.name for font in fonts.fontManager.ttflist}
    candidates = [name for name in ("Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "SimSun") if name in available]
    plt.rcParams["font.sans-serif"] = candidates + ["DejaVu Sans"]
    plt.rcParams["axes.unicode_minus"] = False
    plt.rcParams["toolbar"] = "toolbar2"  # 确保普通图窗提供手动保存按钮。
    maximum = max(max(curve.envelope) for curve in curves)
    y_limit = max(0.1, maximum * 1.08) if common_y_scale else None
    lower, upper = curves[0].zeta, curves[-1].zeta
    initial = (lower + upper) / 2 if initial_zeta is None else initial_zeta
    presets = [curve.zeta for curve in curves] if sweep else [initial]
    for number, zeta in enumerate(presets, 1):
        window = PreviewWindow(config, zeta, lower, upper, number, len(presets), y_limit)
        print(f"[{number}/{len(presets)}] ζ={window.current.zeta:.2f}，seed={config.seed}")
        plt.show(block=True)
        plt.close(window.figure)
        config = window.config
        if window.stop_requested:
            break


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--zeta-min", type=float, default=ZETA_MIN)
    parser.add_argument("--zeta-max", type=float, default=ZETA_MAX)
    parser.add_argument("--zeta", type=float, default=INITIAL_ZETA, help="单窗模式的初始阻尼比，默认范围中点")
    parser.add_argument("--sweep", action="store_true", help="按 0.05 逐图展示，每张图也提供滑块与噪声按钮")
    parser.add_argument("--seed", type=int, default=SEED)
    parser.add_argument("--a0", type=float, help="随机加速度半幅，px/s²")
    parser.add_argument("--yt", type=float, help="回正尺度，px")
    parser.add_argument("--compensation", type=float, help="高阻尼驱动增益系数；0 关闭补偿")
    parser.add_argument("--duration-ms", type=float, default=DURATION_MS, help="展示时长，可超过游戏飞行时长以观察自由衰减")
    parser.add_argument("--approach-ms", type=float)
    parser.add_argument("--noise-ms", type=float)
    parser.add_argument("--fade-ms", type=float)
    parser.add_argument("--exit-strength", type=float)
    parser.add_argument("--dive", action="store_true", help="使用俯冲机接敌时长")
    parser.add_argument("--independent-y", action="store_true", help="各图独立缩放纵轴")
    args = parser.parse_args()
    try:
        values = read_game_parameters()
        lower = values["WobbleDampingRatio"] if args.zeta_min is None else args.zeta_min
        upper = float(Decimal(str(values["WobbleDampingRatio"])) + Decimal(str(values["WobbleDampingRandomness"]))) \
            if args.zeta_max is None else args.zeta_max
        zetas = damping_values(lower, upper)
        if args.zeta is not None and (not math.isfinite(args.zeta) or not lower <= args.zeta <= upper):
            raise ValueError("初始阻尼比须在预览范围内。")
        approach = values["DiveApproachDuration" if args.dive else "ApproachDuration"] \
            if args.approach_ms is None else args.approach_ms / 1000.0
        config = MotionConfig(
            values["WobbleAcceleration"] if args.a0 is None else args.a0,
            values["WobbleAmplitude"] if args.yt is None else args.yt,
            values["WobbleNoiseInterval"] if args.noise_ms is None else args.noise_ms / 1000.0,
            values["WobbleFadeDuration"] if args.fade_ms is None else args.fade_ms / 1000.0,
            values["ExitWobbleStrength"] if args.exit_strength is None else args.exit_strength,
            approach, approach + values["ExitDuration"] if args.duration_ms is None else args.duration_ms / 1000.0,
            args.seed,
            values["WobbleCompensationReference"],
            values["WobbleDampingCompensation"] if args.compensation is None else args.compensation,
        )
        numeric = (config.a0, config.yt, config.noise_interval, config.fade_duration,
                   config.exit_strength, config.approach_duration, config.duration,
                   config.damping_reference, config.damping_compensation)
        if not all(math.isfinite(value) and value >= 0 for value in numeric) or config.duration == 0:
            raise ValueError("物理参数须为非负有限数，展示时长须大于零。")
        if not 0 <= config.exit_strength <= 1:
            raise ValueError("拉升驱动比例须在 [0,1] 内。")
    except (OSError, ValueError) as error:
        parser.error(str(error))
    print(f"读取参数：{SETTINGS_PATH}\n预览范围 [{lower:g}, {upper:g}]；滑块步长 0.01，R/按钮重新生成噪声，Q/Esc 结束。")
    print("欠阻尼的恒定 ±1 驱动参考线可能交叉；灰色区域为有界噪声的逐时包络。")
    show_curves([simulate(config, zeta) for zeta in zetas], config,
                COMMON_Y_SCALE and not args.independent_y, sweep=args.sweep, initial_zeta=args.zeta)


if __name__ == "__main__":
    # Windows 重定向时可能回落到 GBK；帮助文本中的希腊字母、上标统一按 UTF-8 输出。
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    try:
        main()
    except KeyboardInterrupt:
        pass
