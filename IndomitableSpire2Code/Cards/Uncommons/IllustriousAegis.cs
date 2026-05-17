using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

// 指定为 AnyPlayer，支持联机模式下套给自己或队友
public sealed class IllustriousAegis() : IndomitableCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new ShieldVar(12M, ValueProp.Move),
        new HealVar(4M)
    ];
    
    // 手动补充提示框，因为 ShieldVar 默认只给原版的格挡提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ShieldPower>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 1. 赋予护盾（底层会自动给真实格挡并挂上 ShieldPower）
        var shieldAmount = await CustomCreatureCmd.GainShield(cardPlay.Target, DynamicVars.Shield(), cardPlay);
        
        // 2. 赋予光辉的庇护能力（将其层数设为刚才获得的护盾值）
        await PowerCmd.Apply<IllustriousAegisPower>(
            target: cardPlay.Target,
            amount: shieldAmount, 
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：格挡护盾变厚，击碎回血增加
        DynamicVars.Shield().UpgradeValueBy(4M);
        DynamicVars.Heal.UpgradeValueBy(2M);
    }
}