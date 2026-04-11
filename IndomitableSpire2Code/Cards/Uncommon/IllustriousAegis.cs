using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndomitableSpire2.IndomitableSpire2Code.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommon;

public sealed class IllustriousAegis() : IndomitableCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    // 指定为 AnyPlayer，完美支持联机模式下套给队友
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12M, ValueProp.Move),
        new HealVar(5M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 1. 先赋予基础真实的格挡
        var blockAmount = await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay);
        
        // 2. 赋予光辉的庇护能力（实例初始化）
        await PowerCmd.Apply<IllustriousAegisPower>(
            target: cardPlay.Target,
            amount: blockAmount, // 将卡牌带来的格挡值作为护盾层数
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：格挡护盾变厚，击碎回血增加
        DynamicVars.Block.UpgradeValueBy(4M);
        DynamicVars.Heal.UpgradeValueBy(2M);
    }
}