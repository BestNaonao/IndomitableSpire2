using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class AzureCruiser() : TashkentCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<TorpedoPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Owner?.Creature == null) return;

        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        var powerInstance = await PowerCmd.Apply<AzureCruiserPower>(base.Owner.Creature, 1m, base.Owner.Creature, this);
        if (powerInstance != null)
        {
            powerInstance.SetTorpedoPower(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        }
    }
    
    protected override void OnUpgrade(){
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(6M);
    }
}