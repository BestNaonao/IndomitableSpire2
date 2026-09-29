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
    private const decimal BaseValue = 8m;
    
    public decimal ModifiedPositiveValue => ModifyOrbValue(BaseValue);
    // 被动效果数值，ModifyOrbValue表示是否吃集中等
    public override decimal PassiveVal => Math.Max(BaseValue - (ModifiedPositiveValue - BaseValue), 0m);

    // 激发效果数值
    public override decimal EvokeVal => ModifyOrbValue(6);

    // 暗色，使用球的主体色的暗色调
    public override Color DarkenedColor => new(0.4f, 0.2f, 0.5f);

    // 不出现在随机球池中
    public override bool IncludeInRandomPool => false;
    
    // 提示图标路径
    public override string? CustomIconPath => "res://TashkentSpire2/images/orbs/module_orb.png";
    public override string CustomPassiveSfx => "res://TashkentSpire2/sfx/tashkent_PassiveSfx.mp3";
    public override string CustomEvokeSfx => "res://TashkentSpire2/sfx/tashkent_EvokeSfx.mp3";
    public override string CustomChannelSfx => "res://TashkentSpire2/sfx/tashkent_ChannelSfx.mp3";
    
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