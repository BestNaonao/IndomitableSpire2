using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class UnfulfilledVow() : IndomitableCard(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private LocString FulfillDialogue => new("cards", $"{Id.Entry}.banter");
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Resolve>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(24M, ValueProp.Move)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        // 播放角色台词和音频
        CustomTalkCmd.Play(FulfillDialogue, Owner.Creature, VfxColor.Gold)
            .WithExactDuration(2.6d)
            .Execute();
        SfxCmd.Play("res://IndomitableSpire2/sfx/characters/indomitable/link2.wav");
        // 造成大量伤害并播放重击动画
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }
    
    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != PileType.Hand || CombatState is not { } combatState) return;
        var resolve = combatState.CreateCard<Resolve>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            resolve, PileType.Draw, Owner, CardPilePosition.Top));
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8M);
    }
}