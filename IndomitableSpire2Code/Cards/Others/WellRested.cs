using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(StatusCardPool))]
public sealed class WellRested() : IndomitableSpire2Card(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    // 状态牌通常不能升级
    public override int MaxUpgradeLevel => 0;
    
    // 关键字：不能被打出、保留
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Retain];
    
    // 注册能量变量：2点能量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    
    // 核心机制：抽到这张牌时触发
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 确保是自己被抽到
        if (card != this) return;
        // 稍作等待，让卡牌飞入手牌的动画播放一下，避免特效突兀，然后获得对应的能量
        await Cmd.Wait(0.25f);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}