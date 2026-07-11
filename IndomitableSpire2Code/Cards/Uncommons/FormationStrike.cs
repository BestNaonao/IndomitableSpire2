using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class FormationStrike() : IndomitableCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    
    // 悬停提示：展示编队关键字的解释
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.Formation),
        HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9M, ValueProp.Move)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        // 2. 使用 GetByPriority 按优先级从抽牌堆中获取目标
        var drawPile = PileType.Draw.GetPile(Owner).Cards;
        var validCards = drawPile.GetByPriority(
            c => c is CarrierAircraftCard && !c.Keywords.Contains(IndomitableKeywords.Formation),
            c => c.IsCarrierAircraft() && !c.Keywords.Contains(IndomitableKeywords.Formation)
        ).ToList();
        
        // 3. 随机选中一张
        if (validCards.Count > 0)
        {
            var selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(validCards);
            if (selectedCard != null)
            {
                // 4. 调用引擎指令赋予关键字，这会自动在对应卡牌上生效，并处理后续的 UI 渲染
                CardCmd.ApplyKeyword(selectedCard, IndomitableKeywords.Formation);
                // 5. 将这张牌在屏幕中央预览闪烁一下（极佳的视觉反馈）
                CardCmd.Preview(selectedCard);
            }
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3M);
}