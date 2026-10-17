using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class LendLeaseAct : IndomitableRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.TypeSynergy),
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromCard<Xtb2d1Skypirate>(),
        HoverTipFactory.FromCard<Ad1Skyraider>(),
        HoverTipFactory.FromCard<Xf5uFlyingFlapjack>()
    ];
    
    // 用字段记录选择的卡牌，用 null 表示没有选择的卡牌，或选择的卡牌已经被打出，或不在战斗中。
    private WhiteEagleAircraftCard? _pendingAircraft;
    
    public override async Task BeforeCombatStart()
    {
        _pendingAircraft = null;
        var combat = Owner.Creature.CombatState;
        if (combat == null) return;
        var choices = new CardModel[]
        {
            combat.CreateCard<Xtb2d1Skypirate>(Owner),
            combat.CreateCard<Ad1Skyraider>(Owner),
            combat.CreateCard<Xf5uFlyingFlapjack>(Owner)
        };
        var selected = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), 
            choices, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected is not WhiteEagleAircraftCard aircraft) return;
        _pendingAircraft = aircraft;
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(aircraft, PileType.Draw, Owner, CardPilePosition.Random);
    }
    
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != _pendingAircraft || card.Owner != Owner) return;
        _pendingAircraft = null;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    
    public override Task AfterCombatEnd(CombatRoom room)
    {
        _pendingAircraft = null;
        return Task.CompletedTask;
    }
}