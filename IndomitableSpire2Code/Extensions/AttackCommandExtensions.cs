using System.Reflection;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class AttackCommandExtensions
{
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