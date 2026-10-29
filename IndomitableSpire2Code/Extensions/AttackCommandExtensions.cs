using System.Reflection;
using IndomitableSpire2.IndomitableSpire2Code.Animation;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class AttackCommandExtensions
{
    /// <summary>
    /// 显式执行舰载机攻击：每次执行只播放一次 Attack 和飞行，保留原命中特效与攻击钩子。
    /// 必须在 FromCard、Targeting、回调等构造完成后调用；原生人物动作始终禁用，由演出回调接管。
    /// 正常返回、提前结束和异常均卸下临时回调、清理随机目标登记，保留调用方原回调。
    /// 每次攻击应新建命令；原版 Results 会累积，本扩展不将命令重置为可复用模板。
    /// 只接受来源为 CarrierAircraftCard 的命令，手写 AttackContext 仍调用母卡的演出方法。
    /// </summary>
    public static Task<AttackCommand> ExecuteWithAircraftFlight(this AttackCommand command,
        PlayerChoiceContext? choiceContext)
    {
        ArgumentNullException.ThrowIfNull(command);
        return AircraftAttackPresentation.Execute(command, choiceContext);
    }
    
    private static readonly MethodInfo DamagePropsSetter =
        typeof(AttackCommand).GetProperty(nameof(AttackCommand.DamageProps),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
            .GetSetMethod(nonPublic: true)
        ?? throw new MissingMethodException(typeof(AttackCommand).FullName, "set_DamageProps");
    
    /// <summary>
    /// 绕过格挡的同时，保留原本的攻击属性，使得能受到能力等加成。
    /// </summary>
    public static AttackCommand WithUnblockableDamage(this AttackCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        DamagePropsSetter.Invoke(command, [command.DamageProps | ValueProp.Unblockable]);
        return command;
    }
}