using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class SeafireFr47() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 机体稍加固，飞行极度平稳
    protected override int MaxDurability { get; set; } = 6;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(4M, ValueProp.Move),
        new RepeatVar(2),
        new CustomPowerVar<VulnerablePower>(1M),
        new CustomPowerVar<OnFirePower>(1M),
        new ReconVar(4M)
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        
        var recon = await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
        // 按本次侦察实际看到的全部牌计算，不受玩家把牌拿入手、弃置或留顶的选择影响。
        var skills = recon.CardsToHand.Concat(recon.CardsToDiscard)
            .Concat(recon.CardsKept)
            .Count(card => card.Type == CardType.Skill);
        if (skills > 0 && cardPlay.Target.IsAlive)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target,
                skills * DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<OnFirePower>(choiceContext, cardPlay.Target,
                skills * DynamicVars.OnFire().BaseValue, Owner.Creature, this);
        }
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars.Recon().UpgradeValueBy(2M); // 升级后侦察深度变强
        UpgradeDurability();
    }
}