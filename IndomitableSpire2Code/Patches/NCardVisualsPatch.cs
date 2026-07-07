using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NCard))]
public static class NCardVisualsPatch
{
    // Logo 配置
    private const string LogoNodeName = "IndomitableRoyalLogo";
    private const string LogoTexturePath = "res://IndomitableSpire2/images/card_frame/Royal75.png";
    
    // 耐久度 UI 配置
    private const string DurabilityIconName = "IndomitableDurabilityIcon";
    private const string DurabilityLabelName = "IndomitableDurabilityLabel";
    private const string DurabilityIconPath = "res://IndomitableSpire2/images/combatui/durability_panel2.png";
    private const float IconSize = 75f;
    
    // 阴影与描边常量 (参照原版 UI 风格)
    private static readonly StringName ShadowOffsetX = "shadow_offset_x";
    private static readonly StringName ShadowOffsetY = "shadow_offset_y";
    private static readonly StringName ShadowOutlineSize = "shadow_outline_size";
    
    // 缓存贴图避免重复加载
    private static Texture2D? _logoTexture;
    private static Texture2D? _durabilityTexture;
    
    // 其他配置
    private const ResourceLoader.CacheMode CacheMode = ResourceLoader.CacheMode.Ignore;
    
    /// <summary>
    /// 1. 节点初始化 (创建 UI)：在卡牌对象池创建 NCard 时，将节点挂载进去
    /// </summary>
    [HarmonyPatch(nameof(NCard._Ready))]
    [HarmonyPostfix]
    public static void ReadyPostfix(NCard __instance)
    {
        // 获取卡牌容器和底层的卡框节点
        var container = __instance.GetNode<Control>("%CardContainer");
        var frame = __instance.GetNode<TextureRect>("%Frame");
        if (container == null || frame == null) return;
        
        // 1. 初始化 Logo
        var logoNode = EnsureNodeExists(container, LogoNodeName, LoadLogoTexture());
        container.MoveChild(logoNode, frame.GetIndex() + 1);
        
        var logoSize = new Vector2(144, 144);   // 指定大小
        logoNode.CustomMinimumSize = logoSize;
        logoNode.Size = logoSize;
        logoNode.SetAnchorsPreset(Control.LayoutPreset.CenterTop);  // 水平居中
        logoNode.Position = new Vector2(-logoSize.X / 2f, 36f);     // 位置微调
        
        // 2. 初始化耐久度图标与文字 (放置在右上角)
        var durabilityNode = EnsureNodeExists(container, DurabilityIconName, LoadDurabilityTexture());
        // 卡牌大小约为 300x422，中心点为 (0,0)。右上角大约在 X: 70~100, Y: -200~-170
        durabilityNode.Position = new Vector2(96f, -234f);
        durabilityNode.Size = new Vector2(IconSize, IconSize);
        durabilityNode.PivotOffset = durabilityNode.Size * 0.5f;
        
        EnsureLabelExists(durabilityNode);
        
        RefreshAllVisuals(__instance);
    }
    
    /// <summary>
    /// 2. 模型重载 (卡牌刷新逻辑，控制可见性)
    /// </summary>
    [HarmonyPatch("Reload")]
    [HarmonyPostfix]
    public static void ReloadPostfix(NCard __instance) => RefreshAllVisuals(__instance);
    
    /// <summary>
    /// 3. 数值刷新 (更新耐久度文字和颜色)
    /// </summary>
    [HarmonyPatch(nameof(NCard.UpdateVisuals))]
    [HarmonyPostfix]
    public static void UpdateVisualsPostfix(NCard __instance) => RefreshAllVisuals(__instance);
    
    /// <summary>
    /// 核心逻辑：刷新所有自定义 UI
    /// </summary>
    private static void RefreshAllVisuals(NCard cardNode)
    {
        if (!cardNode.IsNodeReady()) return;
        
        var container = cardNode.GetNode<Control>("%CardContainer");
        var logoNode = container?.GetNodeOrNull<TextureRect>(LogoNodeName);
        var durabilityIcon = container?.GetNodeOrNull<TextureRect>(DurabilityIconName);
        var durabilityLabel = durabilityIcon?.GetNodeOrNull<Label>(DurabilityLabelName);
        
        // 1. 刷新 Logo 逻辑
        if (logoNode != null)
        {
            logoNode.Visible = cardNode is
            {
                Model: { Pool: IndomitableCardPool, Rarity: not CardRarity.Ancient },
                Visibility: ModelVisibility.Visible
            };
        }
        
        // 2. 刷新耐久度逻辑
        if (durabilityIcon != null && durabilityLabel != null)
        {
            // 如果卡牌不可见，或者没有耐久度变量，隐藏 UI
            if (cardNode is { Visibility: ModelVisibility.Visible, Model: {} model } && model.HasDurability())
            {
                durabilityIcon.Visible = true;
                
                // 获取当前值和最大值
                var current = (int)model.DynamicVars.Durability().BaseValue;
                var max = (int)model.DynamicVars.MaxDurability().BaseValue;
                durabilityLabel.Text = $"{current}/{max}";
                
                // 核心功能：绿-黄-红 丝滑渐变算法
                var pct = max > 0 ? Mathf.Clamp((float)current / max, 0f, 1f) : 0f;
                var targetColor =
                    // 大于 50%：在黄色和绿色之间插值，将 0.5~1.0 映射为 0.0~1.0
                    pct > 0.5f ? Colors.DarkOrange.Lerp(Colors.DarkGreen, (pct - 0.5f) * 2f) :
                    // 小于等于 50%：在红色和黄色之间插值，将 0.0~0.5 映射为 0.0~1.0
                    Colors.DarkRed.Lerp(Colors.DarkOrange, pct * 2f);
                
                // 应用颜色 (我们改变描边颜色让其发光，文字主体保持奶油色，这样最符合杀戮尖塔的美术规范)
                durabilityLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, targetColor);
                durabilityLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.cream);
            }
            else
            {
                durabilityIcon.Visible = false;
            }
        }
    }
    
    /// <summary>
    /// 辅助方法：安全的节点创建
    /// </summary>
    private static TextureRect EnsureNodeExists(Control parent, string name, Texture2D? texture)
    {
        var node = parent.GetNodeOrNull<TextureRect>(name);
        if (node != null) return node;
        node = new TextureRect
        {
            Name = name,
            Texture = texture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        parent.AddChild(node);
        return node;
    }
    
    private static void EnsureLabelExists(TextureRect icon)
    {
        if (icon.HasNode(DurabilityLabelName)) return;
        
        var label = new Label
        {
            Name = DurabilityLabelName,
            Position = new Vector2(0f, 20f),    // 居中稍微偏下一点，给图标留出空间
            Size = new Vector2(IconSize, IconSize),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = "0/0"
        };
        
        // 描边与阴影设置
        label.AddThemeConstantOverride(ShadowOffsetX, 2);
        label.AddThemeConstantOverride(ShadowOffsetY, 2);
        label.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, 10);
        label.AddThemeConstantOverride(ShadowOutlineSize, 10);
        label.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 20);
        
        var font = ResourceLoader.Load<Font>("res://themes/kreon_bold_shared.tres", null, CacheMode);
        if (GodotObject.IsInstanceValid(font)) 
            label.AddThemeFontOverride(ThemeConstants.Label.Font, font);
        
        icon.AddChild(label);
    }
    
    private static Texture2D? LoadLogoTexture()
    {
        if (!GodotObject.IsInstanceValid(_logoTexture))
            _logoTexture = ResourceLoader.Load<Texture2D>(LogoTexturePath, null, CacheMode);
        return _logoTexture;
    }
    
    private static Texture2D? LoadDurabilityTexture()
    {
        if (!GodotObject.IsInstanceValid(_durabilityTexture))
            _durabilityTexture = ResourceLoader.Load<Texture2D>(DurabilityIconPath, null, CacheMode);
        return _durabilityTexture;
    }
}