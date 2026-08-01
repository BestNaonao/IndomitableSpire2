using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class EngageOnSightPower : DynamicVarSyncPower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    // 绑定第二数值给 UI 展示
    public string GetSecondAmount() => GetInternalData<Data>().UnrestrictedAmount.ToString();
    
    // 智能切分：如果拥有全局生效层数，则使用进阶版的智能描述
    protected override string SmartDescriptionLocKey => GetInternalData<Data>().UnrestrictedAmount > 0
        ? $"{Id.Entry}.smartDescriptionUpgraded" : $"{Id.Entry}.smartDescription";
    
    // 注册动态变量用于能力文本描述
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("UnrestrictedAmount", 0)];

    protected override void SyncDynamicVars()
    {
        DynamicVars["UnrestrictedAmount"].BaseValue = GetInternalData<Data>().UnrestrictedAmount;
        InvokeDisplayAmountChanged();
    }

    // 【核心分流】：监听层数增加。如果是升级版的卡打出的，增加对应的无限制层数
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount > 0 && cardSource is { IsUpgraded: true })
        {
            GetInternalData<Data>().UnrestrictedAmount += (int)amount;
            SyncDynamicVars();
        }
        return Task.CompletedTask;
    }
    
    // 【核心触发】：抽牌时判定
    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 只有自己抽到攻击牌时才触发
        if (card.Type != CardType.Attack || Owner.Player != card.Owner) return;
        
        // 判定当前是否为“安全的回合进行中”：不是回合开始的系统发牌，且当前回合属于自己
        var isDuringTurn = !fromHandDraw && Owner.CombatState!.CurrentSide == Owner.Side;
        
        // 确定本次抽牌应该触发多少次和手牌数量（实时获取最为准确）
        var triggers = isDuringTurn ? Amount : GetInternalData<Data>().UnrestrictedAmount;
        var handSize = PileType.Hand.GetPile(Owner.Player).Cards.Count;
        if (triggers <= 0 || handSize <= 0) return; // 保险机制
        
        Flash();
        VfxCmd.PlayOnCreatureCenters(CombatState.HittableEnemies, "vfx/vfx_attack_slash");
        SfxCmd.Play("slash_attack.mp3");
        
        // 循环执行伤害结算，分别造成多次伤害以匹配“触发多少次”，不受力量影响
        for (var i = 0; i < triggers; i++)
        {
            await CreatureCmd.Damage(
                choiceContext, CombatState.HittableEnemies, handSize, ValueProp.Unpowered, Owner);
        }
    }
    
    private class Data
    {
        public int UnrestrictedAmount;
    }
}