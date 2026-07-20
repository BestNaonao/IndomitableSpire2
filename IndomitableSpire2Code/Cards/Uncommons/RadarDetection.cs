using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class RadarDetection() : IndomitableCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 添加保留关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    // 添加保留的悬浮提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];
    
    // 注册侦察变量：初始 7 点
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ReconVar(7M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        // 核心机制：执行侦察命令
        await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：侦察深度 +3（变为 10 点）
        DynamicVars.Recon().UpgradeValueBy(3M);
    }
}