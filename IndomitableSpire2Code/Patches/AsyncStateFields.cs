using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

internal static class AsyncStateFields
{
    /// <summary>同时按字段类型和原局部变量名定位，避免把不同循环或状态字段误认成目标。</summary>
    internal static FieldInfo StateField(MethodBase original, string name, Type type) =>
        original.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(field => field.FieldType == type && (field.Name == name || field.Name.StartsWith($"<{name}>")));
    
    /// <summary>生成从当前 MoveNext 状态机读取字段的 IL，不缓存可变指令实例。</summary>
    internal static IEnumerable<CodeInstruction> Load(FieldInfo field)
    {
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Ldfld, field);
    }
}