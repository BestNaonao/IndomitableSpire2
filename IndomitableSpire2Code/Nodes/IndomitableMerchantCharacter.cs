using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

[GlobalClass]
public partial class IndomitableMerchantCharacter : NMerchantCharacter
{
    public override void _Ready()
    {
        // 检查当前节点名称是否包含 "Queen" (不区分大小写)
        var animName = Name.ToString().Contains("RaceQueen", StringComparison.OrdinalIgnoreCase) ? "sit" : "sleep";
        PlayAnimation(animName, true);
    }
}