using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class AirspaceRecon() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ReconVar(0)];
    
    // 声明为 X 费卡牌
    protected override bool HasEnergyCostX => true;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 获取投入的 X 能量
        var xValue = ResolveEnergyXValue();
        
        // 2. 计算侦察总深度：3X (如果升级了，再加3)
        var reconAmount = xValue * 3 + DynamicVars.Recon().IntValue;
        if (reconAmount <= 0) return;
        
        // 3. 执行侦察，并获取返回的指令结果对象
        var reconCmd = await ReconCommand.Recon(Owner, reconAmount).Execute(choiceContext);
        
        // 4. 如果未升级，免费 1 张；如果已升级，免费 X 张（至少1张，防止 X=0 导致升级版反而不如未升级版），截取放入手牌的列表中最前面的几张
        if (reconCmd.CardsToHand.Count > 0)
            foreach (var card in reconCmd.CardsToHand.Take(IsUpgraded ? Math.Max(1, xValue) : 1))
                card.SetToFreeThisTurn();
    }
    
    // 侦查变量升级加3
    protected override void OnUpgrade() => DynamicVars.Recon().UpgradeValueBy(3M);
}