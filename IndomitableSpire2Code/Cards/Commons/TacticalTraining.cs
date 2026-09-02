using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class TacticalTraining() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 添加消耗关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 使用 MagicVar 控制层数，基础 20 层，升级 30 层
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("SpecialPowerAmount", 20M)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft), HoverTipFactory.FromPower<AviationPower>()];
    
    // 【核心限制】：只有手牌中包含至少一张带有“舰载机”标签的牌时，此卡才亮起可打出
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(c => c.IsCarrierAircraft());
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 创建选择器配置
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        
        // 调用原版的手牌选择指令。过滤器：只允许选择舰载机
        var selectedCard = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, 
            c => c.IsCarrierAircraft(), this)).FirstOrDefault();
        
        if (selectedCard != null)
        {
            var amount = DynamicVars["SpecialPowerAmount"].BaseValue;
            // 根据选中的舰载机标签，精准投放对应的航空能力
            if (selectedCard.IsAircraftType(IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter))
                await PowerCmd.Apply<AirCombatElitePower>(choiceContext: choiceContext, Owner.Creature, amount, Owner.Creature, this);
            else if (selectedCard.IsAircraftType(IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber))
                await PowerCmd.Apply<TorpedoMasteryPower>(choiceContext: choiceContext, Owner.Creature, amount, Owner.Creature, this);
            else if (selectedCard.IsAircraftType(IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber))
                await PowerCmd.Apply<LethalDivePower>(choiceContext: choiceContext, Owner.Creature, amount, Owner.Creature, this);
            else if (selectedCard.IsAircraftType(IndomitableTags.LevelBomber, IndomitableKeywords.LevelBomber))
                await PowerCmd.Apply<ScorchedBombingPower>(choiceContext: choiceContext, Owner.Creature, amount, Owner.Creature, this);
            // 无特殊分类的舰载机（如水上侦察机、反潜机），提供通用航空 Buff 作为下限保障
            else
                await PowerCmd.Apply<AviationPower>(choiceContext: choiceContext, Owner.Creature, amount, Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["SpecialPowerAmount"].UpgradeValueBy(10M);
    }
}