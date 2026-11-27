using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

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
        
        matcher.Start().Advance(lookup).CreateLabel(out var vanillaLookup);
        // 1. 复制栈顶的 cardModel 引用。因为无论后续分支如何，原版逻辑可能仍需要这个引用。
        // 2. 检查复制的引用是否是 Mod 自定义的 IndomitableSpire2Card 类型。如果是，栈顶变为该类型的引用；如果不是，栈顶变为 null。
        // 3. 如果栈顶为 null（即不是 Mod 的卡牌），则弹出 null，并跳转到 vanillaLookup，执行原版的 if-else 逻辑。
        // 4. 如果是 Mod 的卡牌，则调用下方的 GetHighestDamage 静态方法，传入 cardModel。该方法会返回一个 decimal 类型的伤害值，并将其压入栈顶。
        // 5. 此时栈顶正好是 GetHighestDamage 返回的 decimal 值，完美衔接原版的 StoresLocal 指令，将值赋给 damage1，从而跳过了原版的 if-else 链。
        return matcher.Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Isinst, typeof(IndomitableSpire2Card)),
                new CodeInstruction(OpCodes.Brfalse, vanillaLookup),
                CodeInstruction.Call(typeof(ThrashDamagePatch), nameof(GetHighestDamage)),
                new CodeInstruction(OpCodes.Br, storeDamage))
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
        Log.Warn($"Thrash exhausted attack card {card.Id.Entry} that did not have an appropriate damage var!");
        return 0m;
    }
}