using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Swordfish818Squadron() : CarrierAircraftCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 精锐中队，机动性更强，但在高烈度对抗中耐久上限略低以平衡强度
    protected override int MaxDurability { get; set; } = 8;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.TorpedoBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(11M, ValueProp.Move),
        new PowerVar<FloodingPower>(3M),
        new PowerVar<SlowPower>(1M) // 用于瘫痪敌方攻势的缓慢变量，层数设为 1
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        if (cardPlay.Target is { IsAlive: true })
        {
            await PowerCmd.Apply<FloodingPower>(
                target: cardPlay.Target,
                amount: DynamicVars["FloodingPower"].BaseValue,
                applier: Owner.Creature,
                cardSource: this
            );
            
            // 历史致敬：击毁尾舵。如果目标意图攻击或逃跑，赋予缓慢卡死其机动性。
            if (cardPlay.Target.Monster is { } monster && (monster.IntendsToAttack || monster.IntendsToEscape()))
            {
                await PowerCmd.Apply<SlowPower>(
                    target: cardPlay.Target,
                    amount: DynamicVars["SlowPower"].BaseValue,
                    applier: Owner.Creature,
                    cardSource: this
                );
            }
        }
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4M);
        DynamicVars["FloodingPower"].UpgradeValueBy(1M);
        UpgradeDurability();
        // 缓慢层数保持 1 层不变
    }
}