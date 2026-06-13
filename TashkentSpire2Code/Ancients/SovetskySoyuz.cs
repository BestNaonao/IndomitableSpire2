using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using TashkentSpire2.TashkentSpire2Code.Relics;

namespace TashkentSpire2.TashkentSpire2Code.Ancients;

public sealed class SovetskySoyuz : CustomAncientModel
{
    public override bool IsValidForAct(ActModel act) =>
        act.Id == ModelDb.Act<Hive>().Id || act.Id == ModelDb.Act<Glory>().Id;
    
    public override string? CustomScenePath => "res://TashkentSpire2/scenes/ancients/SovetskySoyuz.tscn";
    
    public override string? CustomMapIconPath => "res://TashkentSpire2/images/ancients/Sovetsky_MapIcon.png";

    public override string? CustomMapIconOutlinePath => "res://TashkentSpire2/images/ancients/SovetskySoyuz_MapIconOutline.png";

    public override string? CustomRunHistoryIconPath => "res://TashkentSpire2/images/ancients/SovetskySoyuz_RunHistoryIcon.png";

    public override string? CustomRunHistoryIconOutlinePath => "res://TashkentSpire2/images/ancients/SovetskySoyuz_RunHistoryIconOutline.png";

    protected override OptionPools MakeOptionPools => new(
        MakePool(
            ModelDb.Relic<SovetskyUnion>(),
            ModelDb.Relic<PreferredPlan>(),
            ModelDb.Relic<ShortTermDamageControl>()
        ),
        MakePool(
            ModelDb.Relic<OrderOfSolidarity>(),
            ModelDb.Relic<CommissarHat>(),
            ModelDb.Relic<TheLastShell>(),
            ModelDb.Relic<AbsolutVodka>()
        ),
        MakePool(
            ModelDb.Relic<ArcticHare>(),
            ModelDb.Relic<KGB>(),
            ModelDb.Relic<BearClawGloves>()
        )
    );
}
