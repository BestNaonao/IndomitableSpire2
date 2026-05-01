using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

[GlobalClass]
public partial class IndomitableMerchantCharacter : NMerchantCharacter
{
    public override void _Ready()
    {
        // 覆盖播放我们的 sleep 动画
        PlayAnimation("sleep", true);
    }
}