using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class TorpedoSea() : TashkentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private int _timesPlayedThisCombat;
    
    private int TimesPlayedThisCombat
    {
        get
        {
            return _timesPlayedThisCombat;
        }
        set
        {
            AssertMutable();
            _timesPlayedThisCombat = value;
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M),
        new RepeatVar(3)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner.Creature, this);
        }
        TimesPlayedThisCombat++;
        base.EnergyCost.AddThisCombat(-1);
    }
    
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1M);
}