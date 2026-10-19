using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Ancients;

// 事件卡池与先古稀有度相互独立：不进入不挠的普通战斗卡牌奖励。
[Pool(typeof(EventCardPool))]
public sealed class CreedOfReprisal() : IndomitableSpire2Card(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<CreedOfReprisalPower>(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        await PowerCmd.Apply<CreedOfReprisalPower>(choiceContext, Owner.Creature,
            DynamicVars["CreedOfReprisalPower"].BaseValue, Owner.Creature, this);
    }
    
    // 升级添加固有
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}