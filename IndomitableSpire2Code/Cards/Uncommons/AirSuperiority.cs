using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class AirSuperiority() : IndomitableCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.CarrierAircraft];
    
    protected override HashSet<CardTag> CanonicalTags => [IndomitableTags.CarrierAircraft];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DamageVar(6M, ValueProp.Move),
        new CalculationBaseVar(1M),
        new CalculationExtraVar(1M),
        new CalculatedVar("CalculatedHits").WithMultiplier((card, _) => 
        {
            if (card.CombatState == null) return 1M; // 不在战斗中默认显示 1 次
            
            // 1. 获取本场战斗中该玩家打出的舰载机牌数量
            var aircraftPlayedCount = CombatManager.Instance.History.Entries
                .OfType<CardPlayFinishedEntry>()
                .Count(e => e.CardPlay.Card.Owner == card.Owner && e.CardPlay.Card.IsCarrierAircraft());
            
            // 2. 获取当前存活且可被选中的敌人数量
            var enemyCount = card.CombatState.HittableEnemies.Count;
            
            // 3. 计算实际额外打击次数：Max(0, 舰载机数 - 敌人数)
            return Math.Max(0M, aircraftPlayedCount - enemyCount);
        })
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 执行多段伤害指令， 动态计算本次打出的实际段数
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount((int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target))
            .FromCard(this, cardPlay)
            .OnlyPlayAnimOnce()
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：伤害 +3 (变为 11 点)
        DynamicVars.Damage.UpgradeValueBy(3M);
    }
}