using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NCard))]
public static class NCardLogoPatch
{
    // Logo 节点在 Godot 场景树中的名字与 Logo 的材质路径
    private const string LogoNodeName = "IndomitableRoyalLogo";
    private const string LogoTexturePath = "res://IndomitableSpire2/images/card_frame/Royal75.png";
    
    /// <summary>
    /// 1. 拦截节点初始化：在卡牌对象池创建 NCard 时，将 Logo 节点挂载进去
    /// </summary>
    [HarmonyPatch(nameof(NCard._Ready))]
    [HarmonyPostfix]
    public static void ReadyPostfix(NCard __instance)
    {
        // 获取卡牌容器和底层的卡框节点
        var container = __instance.GetNode<Control>("%CardContainer");
        var frame = __instance.GetNode<TextureRect>("%Frame");
        if (container == null || frame == null) return;
        
        // 创建专属的 Logo 贴图节点
        var logoNode = new TextureRect
        {
            Name = LogoNodeName,
            Texture = ResourceLoader.Load<Texture2D>(LogoTexturePath),
            MouseFilter = Control.MouseFilterEnum.Ignore, 
            Visible = false, 
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        
        // 挂载到卡牌容器中，并将其移动到 Frame 节点之后。
        container.AddChild(logoNode);
        container.MoveChild(logoNode, frame.GetIndex() + 1);
        
        // 显式指定大小
        var logoSize = new Vector2(144, 144); 
        logoNode.CustomMinimumSize = logoSize;
        logoNode.Size = logoSize;
        
        // 布局设置: 将锚点设置在卡牌的“顶部中心”，水平居中，并只调节 Y 轴高度
        logoNode.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        
        // 因为锚点在中心，X 轴需要向左偏移自身宽度的一半，才能实现完美的水平居中
        logoNode.Position = new Vector2(-logoSize.X / 2f, 36f);
        
        // 因为官方在 _Ready 内部调用的 Reload 错过了这个节点，我们在这里手动补充调用一次可见性判定逻辑！
        UpdateLogoVisibility(__instance, logoNode);
    }
    
    /// <summary>
    /// 2. 拦截卡牌刷新逻辑：判断当前装载的卡牌模型是否属于“不挠”，从而决定是否显示 Logo
    /// 注意：Reload 是 private 方法，我们可以直接用字符串名称指定
    /// </summary>
    [HarmonyPatch("Reload")]
    [HarmonyPostfix]
    public static void ReloadPostfix(NCard __instance)
    {
        // 确保节点就绪且已装载卡牌模型
        if (!__instance.IsNodeReady() || __instance.Model == null) return;
        
        var container = __instance.GetNode<Control>("%CardContainer");
        var logoNode = container?.GetNodeOrNull<TextureRect>(LogoNodeName);
        
        if (logoNode != null)
        {
            UpdateLogoVisibility(__instance, logoNode);
        }
    }
    
    // 抽离出来的公共可见性判定逻辑
    // - 这张卡牌必须属于你的角色卡池
    // - 卡牌必须是可见状态（不能是背面或锁住状态）
    // - 卡牌不能是远古稀有度（Ancient 卡牌会隐藏标准卡框，使用自己的全画幅背景）
    private static void UpdateLogoVisibility(NCard cardNode, TextureRect logoNode)
    {
        if (cardNode.Model == null) return;
        logoNode.Visible = cardNode.Model.Pool is IndomitableCardPool && 
                           cardNode.Visibility == ModelVisibility.Visible && 
                           cardNode.Model.Rarity != CardRarity.Ancient;
    }
}