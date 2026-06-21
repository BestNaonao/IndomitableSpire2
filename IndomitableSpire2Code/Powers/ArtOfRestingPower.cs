using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ArtOfRestingPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    
    // 该能力无需堆叠层数，有与无的区别即可
    public override PowerStackType StackType => PowerStackType.Single;
    
    // 核心钩子：在生成卡牌后拦截
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        // 如果不是能力拥有者的玩家生成的、不是玩家的牌、不是状态牌，直接放行
        if (card.Type != CardType.Status || creator == null || creator != Owner.Player || card.Owner != creator)
            return;
        
        // 闪烁并等待一下
        Flash();
        await Cmd.Wait(0.2f);
        // 生成一张“养神”，并将生成的状态牌转化为它
        var refreshCard = CombatState.CreateCard<Refresh>(Owner.Player);
        await CardCmd.Transform(card, refreshCard);
        await Cmd.Wait(0.2f);
    }
}