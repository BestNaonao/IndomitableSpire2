using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Orb;

public sealed class ModuleOrb : CustomOrbModel
{
    private const decimal BaseValue = 10m;
    
    public decimal ModifiedPositiveValue => ModifyOrbValue(BaseValue);
    // 被动效果数值，ModifyOrbValue表示是否吃集中等
    public override decimal PassiveVal => Math.Max(BaseValue - (ModifiedPositiveValue - BaseValue), 0m);

    // 激发效果数值
    public override decimal EvokeVal => ModifyOrbValue(4);

    // 暗色，使用球的主体色的暗色调
    public override Color DarkenedColor => new(0.4f, 0.2f, 0.5f);

    // 不出现在随机球池中
    public override bool IncludeInRandomPool => false;
    
    // 提示图标路径
    public override string? CustomIconPath => "res://TashkentSpire2/images/orbs/module_orb.png";
    // 球的场景的路径。如果你使用这个，你必须要有一个名称为SpineSkeleton并且是SpineSprite类型的节点
    // public override string? CustomSpritePath => "res://test/scenes/test_orb.tscn";

    // 可以继承这个并自行搭建场景，只需父节点是Node2D即可。这样就没有上述限制。代码上优先使用这个
    public override Node2D? CreateCustomSprite()
    {
        return PreloadManager.Cache.GetScene("res://TashkentSpire2/scenes/orbs/module_orb.tscn").Instantiate<Node2D>();
    }

    // // 回合开始时触发被动
    // public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext choiceContext)
    // {
    //     await Passive(choiceContext, null);
    // }
    
    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == this.Owner.Creature && amount > PassiveVal)
        {
            return PassiveVal;
        }

        return amount;
    }

    // // 触发被动
    // public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    // {
    //     Trigger();
    //     await CardPileCmd.Draw(choiceContext, PassiveVal, Owner);
    // }

    // 触发激发，返回受影响的角色
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext playerChoiceContext)
    {
        PlayEvokeSfx();
        await CreatureCmd.Heal(base.Owner.Creature, EvokeVal);
        return [Owner.Creature];
    }
}