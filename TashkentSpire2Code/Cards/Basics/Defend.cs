using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Basics;

public sealed class Defend() : TashkentCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    
    // 定义卡牌数值：5点格挡。STS2 使用 BlockVar
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 调用格挡指令，给玩家自身增加护甲
        var num = await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (num < DynamicVars.Block.BaseValue)
        {
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(1M, ValueProp.Unpowered), cardPlay);
        }
    }

    // 升级逻辑：格挡数值提升 3 点
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}