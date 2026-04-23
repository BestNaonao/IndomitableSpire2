using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
public static class MerchantAnimationPatch
{
    // 使用 ref 关键字，允许我们在原方法执行前修改传入的参数
    [HarmonyPrefix]
    public static void Prefix(NMerchantCharacter __instance, ref string anim)
    {
        // 检查当前节点的名字是否为我们的专属商人节点
        if (__instance.Name == "IndomitableMerchant" || __instance.Name == "IndomitableMaidMerchant")
        {
            // 如果官方脚本试图播放硬编码的 relaxed_loop，我们将其拦截并替换为 sleep
            if (anim == "relaxed_loop")
            {
                anim = "sleep";
            }
        }
    }
}