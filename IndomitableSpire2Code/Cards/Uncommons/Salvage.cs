using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Salvage() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(IndomitableKeywords.Durability)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        var selectedCards = (await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Exhaust.GetPile(Owner), Owner, 
            new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue)
        )).ToList();
        // 选择的牌会移除虚无关键词并且恢复耐久度
        foreach (var card in selectedCards)
        {
            // 移除关键词，修复，加入抽牌堆
            CardCmd.RemoveKeyword(card, CardKeyword.Ethereal);
            await RepairCmd.FullyRepair(card);
        }
        await CardPileCmd.Add(selectedCards, PileType.Draw, CardPilePosition.Random);
    }
    
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}