using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class ArtOfResting() : IndomitableCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private LocString RestingDialogue => new("cards", $"{Id.Entry}.banter");
    
    // 提供完善的提示窗
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromCard<Indolent>(),
        HoverTipFactory.FromCard<Refresh>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.PlayerCombatState is null) return;
        
        // 1. 播放施法动画，以及播放台词
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        TalkCmd.Play(RestingDialogue, Owner.Creature, VfxColor.Gold, VfxDuration.VeryLong);
        
        // 2. 极其优雅地全堆查找：遍历该玩家所有牌堆（抽牌、弃牌、手牌、消耗），找到所有“慵懒”，将之前积攒的“慵懒”统统转化为“养神”
        var indolentCards = Owner.PlayerCombatState.AllPiles
            .SelectMany(p => p.Cards)
            .Where(c => c is Indolent)
            .ToList();
        foreach (var original in indolentCards)
            await CardCmd.Transform(original, CombatState!.CreateCard<Refresh>(Owner));
        
        // 3. 赋予休息的艺术能力，处理未来生成的状态牌
        await PowerCmd.Apply<ArtOfRestingPower>(Owner.Creature, 1M, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        // 升级将费用从 2 降为 1
        EnergyCost.UpgradeBy(-1);
    }
}