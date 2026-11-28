using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ModCard = IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts.IndomitableSpire2Card;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>其他 Mod 可复制本文件到自己的命名空间，并将 ModCard 指向自己的卡牌基类。</summary>
[HarmonyPatch(typeof(Thrash), "OnPlay", MethodType.Async)]
internal static class ThrashDamagePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // 定位代码 if (cardModel.DynamicVars.ContainsKey("CalculatedDamage")) 的位置
        var matcher = new CodeMatcher(instructions, generator)
            .MatchStartForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.DynamicVars))),
                new CodeMatch(OpCodes.Ldstr, CalculatedDamageVar.defaultName),
                CodeMatch.Calls(AccessTools.Method(typeof(DynamicVarSet), nameof(DynamicVarSet.ContainsKey))))
            .ThrowIfInvalid("Thrash: could not locate the exhausted card's damage lookup.");
        var lookup = matcher.Pos;
        
        // 定位“damage1 =”对应的 IL 代码，添加标签。两条路径共用原版存入 damage 的指令，后续伤害修正、成长记录和消耗均由痛殴结算。
        matcher.MatchStartForward(
                CodeMatch.Calls(AccessTools.Method(typeof(CalculatedVar), nameof(CalculatedVar.Calculate))),
                CodeMatch.StoresLocal())
            .ThrowIfInvalid("Thrash: could not locate the damage assignment.")
            .Advance().CreateLabel(out var storeDamage);
        
        matcher.Start().Advance(lookup);
        // 旧版补丁可能直接跳到 get_DynamicVars。把已有入口标签移到 dup，让它也能进入本段判断。
        // 假设第二个 Mod 尝试用相同的方法移动标签，会因为第一个 Mod 已经将标签移走而没有标签，此后插入到原版指令上面、第一个 Mod 后面才是正确的。
        var entry = new CodeInstruction(OpCodes.Dup).MoveLabelsFrom(matcher.Instruction);
        var next = generator.DefineLabel();
        // 类型不匹配时，原卡牌仍在栈顶。从本段末尾的 nop 顺序进入下一份补丁或原版取值逻辑。
        return matcher.Insert(
                entry,
                new CodeInstruction(OpCodes.Isinst, typeof(ModCard)), // 消耗 Dup 复制的栈顶，做出类型判断
                new CodeInstruction(OpCodes.Brfalse, next), // 类型不匹配，跳转到 next 标签对应的行
                CodeInstruction.Call(typeof(ThrashDamagePatch), nameof(GetHighestDamage)), // 类型匹配，消耗原栈顶做自定义计算
                new CodeInstruction(OpCodes.Br, storeDamage), // 跳转到原版赋值语句
                new CodeInstruction(OpCodes.Nop).WithLabels(next)) // 添加标签，作为放行落脚点，栈顶: cardModel
            .InstructionEnumeration();
    }
    
    private static decimal GetHighestDamage(CardModel card)
    {
        var highest = card.DynamicVars.Values.Max(variable => variable switch
        {
            CalculatedDamageVar calculated => calculated.Calculate(null),
            DamageVar or OstyDamageVar => variable.BaseValue,
            _ => null
        });
        if (highest.HasValue) return highest.Value;
        // 类型匹配失败，复用原版告警逻辑。
        Log.Warn($"Thrash exhausted attack card {card.Id.Entry} that did not have an appropriate damage var!");
        return 0m;
    }
}