using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Basics;

public sealed class Strike() : TashkentCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    // 标记为“打击”牌，使其能受到完美打击等遗物/卡牌的加成
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 定义卡牌数值：6点伤害。注意 STS2 统一使用 decimal 类型，即数字后带 'M'
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6M, ValueProp.Move)];

    // 出牌时的异步执行逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        // 调用伤害指令，应用当前的伤害变量，并附带斩击特效
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 升级逻辑：伤害数值提升 3 点
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3M);
}