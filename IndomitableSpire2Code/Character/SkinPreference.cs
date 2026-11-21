using BaseLib.Config;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

internal static class SkinPreference
{
    /// <summary>皮肤偏好记忆。保存到配置项，然后保存配置。</summary>
    internal static void Remember(IndomitableSkin skin)
    {
        IndomitableConfiguration.PreferredSkin = skin;
        ModConfig.SaveDebounced<IndomitableConfiguration>(0);
    }
    
    internal static Indomitable PreferredModel => Model(IndomitableConfiguration.PreferredSkin);
    
    internal static Indomitable Model(IndomitableSkin skin) => skin switch
    {
        IndomitableSkin.Maid => ModelDb.Character<IndomitableMaid>(),
        IndomitableSkin.RaceQueen => ModelDb.Character<IndomitableRaceQueen>(),
        _ => ModelDb.Character<IndomitableDefault>()
    };
    
    internal static void OnRunStarted(RunState run)
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Replay &&
            LocalContext.GetMe(run.Players)?.Character is Indomitable character)
            Remember(character.CurrentSkin);
    }
}