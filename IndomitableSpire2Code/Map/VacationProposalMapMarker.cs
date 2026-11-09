using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

public sealed class VacationProposalMapMarker(IRunState run, MapPoint point) : MapMarker(run, point)
{
    public override string Id => "VacationProposal";
    
    public override string TexturePath => "map_vacation_proposal_marker.png".MapIconPath();
    
    public override bool Visible => VacationProposalService.IsSpecial(Run, Point);
    
    public override string Description => 
        new LocString("relics", "INDOMITABLESPIRE2-VACATION_PROPOSAL.marker").GetFormattedText();
}