using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TauntPower : TashkentPower
{
    private class Data
	{
		public readonly Dictionary<CardModel, int> amountsForPlayedCards = new Dictionary<CardModel, int>();
	}

	public const string DexterityAppliedKey = "DexterityApplied";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType
	{
		get
		{
			if (base.DynamicVars["DexterityApplied"].IntValue != 0)
			{
				return PowerStackType.Counter;
			}
			return PowerStackType.None;
		}
	}
	
	public override string CustomBigIconPath => 
		"res://TashkentSpire2/images/powers/big/taunt_power.png";
	public override string CustomPackedIconPath => 
		"res://TashkentSpire2/images/powers/packed/taunt_power.png";

	public override int DisplayAmount => base.DynamicVars["DexterityApplied"].IntValue;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<DexterityPower>(1m),
		new DynamicVar("DexterityApplied", 0m)
	];

	protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != base.Owner)
		{
			return Task.CompletedTask;
		}
		GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, base.DynamicVars.Dexterity.IntValue);
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == base.Owner.Player && GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out var value))
		{
			Flash();
			await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner, value * this.Amount, base.Owner, null, silent: true);
			base.DynamicVars["DexterityApplied"].BaseValue += (decimal)base.DynamicVars.Dexterity.IntValue * this.Amount;
			InvokeDisplayAmountChanged();
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (side == base.Owner.Side)
		{
			await PowerCmd.Remove(this);
			await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner, -base.DynamicVars["DexterityApplied"].BaseValue, base.Owner, null, silent: true);
		}
	}
}