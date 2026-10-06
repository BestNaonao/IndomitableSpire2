using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// 原版免费购买遗物时跳过本地付款，却仍同步 Cost，导致其他客户端错误扣钱。
// 仅修正同步参数；福袋、领主阳伞等免费领取都适用，商品价格仍由原版计算。
[HarmonyPatch]
public static class MerchantRelicGoldSyncPatch
{
    private static MethodInfo TargetMethod() => AccessTools.AsyncMoveNext(
        AccessTools.DeclaredMethod(typeof(MerchantRelicEntry), "OnTryPurchase",
            [typeof(MerchantInventory), typeof(bool)]));
    
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var code = instructions.ToList();
        var normalize = AccessTools.Method(typeof(MerchantRelicGoldSyncPatch), nameof(GoldToSync));
        if (code.Any(instruction => instruction.Calls(normalize))) return code;
        
        var sync = AccessTools.Method(typeof(RewardSynchronizer), nameof(RewardSynchronizer.SyncLocalGoldLost), [typeof(int)]);
        var sites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.Calls(sync)).ToArray();
        if (sites.Length != 1)
            throw new InvalidOperationException($"商店遗物金币同步补丁：预期一个 SyncLocalGoldLost 调用，实际找到 {sites.Length} 个。");
        
        var ignoreCost = AccessTools.DeclaredField(original.DeclaringType, "ignoreCost");
        if (ignoreCost == null || ignoreCost.FieldType != typeof(bool))
            throw new InvalidOperationException("商店遗物金币同步补丁：无法定位异步状态机的 ignoreCost 字段。");
        
        // 调用前栈上已有 synchronizer、Cost；追加本次交易的 ignoreCost，只替换金额。
        var site = sites[0];
        var loadState = new CodeInstruction(OpCodes.Ldarg_0);
        loadState.MoveLabelsFrom(site.instruction).MoveBlocksFrom(site.instruction);
        code.InsertRange(site.index, [loadState, new CodeInstruction(OpCodes.Ldfld, ignoreCost),
            new CodeInstruction(OpCodes.Call, normalize)]);
        return code;
    }
    
    private static int GoldToSync(int cost, bool ignoreCost) => ignoreCost ? 0 : cost;
}