using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class KenosisFormPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 每层的各项数值基准
    private const int HealPerStack = 1;
    private const int MotivationPerStack = 10;
    private const int CardsPerStack = 1;
    
    // 注册动态变量，依靠生命周期同步
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new HealVar(0M),
        new MotivationGainVar(0M),
        new CardsVar(0)
    ];
    
    // 同步变量值，让 SmartDescription 能够动态展示叠加后的效果
    private void SyncDynamicVars()
    {
        DynamicVars.Heal.BaseValue = Amount * HealPerStack;
        DynamicVars.MotivationGain().BaseValue = Amount * MotivationPerStack;
        DynamicVars.Cards.BaseValue = Amount * CardsPerStack;
    }
    
    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) SyncDynamicVars();
        return Task.CompletedTask;
    }
    
    // 机制一：回合开始时回血
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0) return;
        Flash();
        await CreatureCmd.Heal(Owner, DynamicVars.Heal.BaseValue);
    }
    
    // 机制二：每次回复生命时，获得干劲并塞入升级版的养神
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner && delta > 0M)
        {
            Flash();
            
            // 获得干劲
            await PowerCmd.Apply<MotivationPower>(
                target: Owner,
                amount: DynamicVars.MotivationGain().BaseValue,
                applier: Owner,
                cardSource: null
            );
            
            // 塞入升级版“养神”
            if (Owner is { Player: not null, CombatState: not null })
            {
                var refreshCards = Owner.CombatState.CreateCards<Refresh>(
                    Owner.Player, DynamicVars.Cards.IntValue).ToList();
                foreach (var card in refreshCards)
                    CardCmd.Upgrade(card); // 赋予升级状态（养神+）
                
                // 放入抽牌堆的随机位置
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(
                    refreshCards, PileType.Draw, true, CardPilePosition.Random));
            }
        }
    }
}