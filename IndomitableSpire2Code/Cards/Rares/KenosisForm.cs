using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class KenosisForm() : IndomitableCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 官方规范：形态牌初始带有虚无，升级后移除
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    // 注册变量：施加 1 层形态能力
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<KenosisFormPower>(1M)];
    
    // 悬浮提示框：展示干劲和我们要塞入的“养神”
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<MotivationPower>(),
        HoverTipFactory.FromCard<Refresh>(true)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 形态牌标准：播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 施加“虚己形态”能力
        await PowerCmd.Apply<KenosisFormPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: DynamicVars["KenosisFormPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除虚无
        RemoveKeyword(CardKeyword.Ethereal);
    }
}