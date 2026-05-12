using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(RestSiteOption), "get_Icon")]
public static class RestSiteOptionIconPatch
{
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance.OptionId == "TASHKENTSPIRE2-LOAD")
        {
            string customPath = "res://TashkentSpire2/images/rest_site/load.png";
            __result = PreloadManager.Cache.GetTexture2D(customPath);
            return false;
        }
        return true;
    }
}