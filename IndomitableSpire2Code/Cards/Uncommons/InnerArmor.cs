using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class InnerArmor() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block), 
        HoverTipFactory.FromPower<ShieldPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 获取打出瞬间当前的格挡值
        var currentBlock = Owner.Creature.Block;
        // 只有当前有格挡时，才挂上“内层装甲”能力（防止给玩家挂个0层没用的Buff）
        if (currentBlock > 0)
        {
            await PowerCmd.Apply<InnerArmorPower>(
                choiceContext: choiceContext, 
                target: Owner.Creature, 
                amount: currentBlock,
                applier: Owner.Creature, 
                cardSource: this
            );
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：对标延伸，移除消耗
        RemoveKeyword(CardKeyword.Exhaust);
    }
}