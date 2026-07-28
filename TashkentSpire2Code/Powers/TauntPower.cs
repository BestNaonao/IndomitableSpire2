using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TauntPower : TashkentPower
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	
	public override string CustomBigIconPath => 
		"res://TashkentSpire2/images/powers/big/taunt_power.png";
	public override string CustomPackedIconPath => 
		"res://TashkentSpire2/images/powers/packed/taunt_power.png";

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == base.Owner.Player)
		{
			Flash();
			await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner, base.Amount, base.Owner, null);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(base.Owner))
			return;
		
		await PowerCmd.Remove(this);
	}
}