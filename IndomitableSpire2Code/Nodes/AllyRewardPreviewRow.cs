using System.Globalization;
using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>一个收奖者的一行提示；内容不变时复用全部节点和纹理。</summary>
public partial class AllyRewardPreviewRow : Control
{
    private AllyRewardPreview[] _rewards = []; // 奖励状态缓存
    
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; // 忽略鼠标点击，以防止 UI 遮挡
        ZIndex = AllyRewardPreviewSettings.ZIndex;
        Modulate = new Color(1F, 1F, 1F, AllyRewardPreviewSettings.Opacity);
    }
    
    public void SetRewards(AllyRewardPreview[] rewards)
    {
        if (_rewards.SequenceEqual(rewards)) return; // C# LINQ 方法，逐元素比较各个奖励预览。记录类型的每个属性也要对应相同。
        _rewards = rewards;
        foreach (var child in GetChildren())
        {
            if (child is CanvasItem canvas) canvas.Hide(); // 先隐藏
            child.QueueFree(); // 后删除所有子节点
        }
        var textures = rewards.Select(reward => (Reward: reward, Texture: reward.IconPath.LoadTexture()))
            .Where(item => item.Texture != null).ToArray(); // 加载材质并过滤掉加载失败的奖励
        Visible = textures.Length > 0;
        if (!Visible) return; // 避免渲染空的奖励节点
        // 计算给左侧加号预留的长度和奖励行的总尺寸
        var iconSize = AllyRewardPreviewSettings.IconSize;
        var start = AllyRewardPreviewSettings.PlusWidth + AllyRewardPreviewSettings.PlusIconGap;
        Size = new Vector2(start + textures.Length * iconSize + (textures.Length - 1) * AllyRewardPreviewSettings.IconGap, iconSize);
        var plus = CreateLabel("+", AllyRewardPreviewSettings.PlusFontSize, 5);
        plus.Name = "Plus";
        plus.Size = new Vector2(AllyRewardPreviewSettings.PlusWidth, iconSize);
        AddChild(plus);
        for (var i = 0; i < textures.Length; i++)
        {
            var (reward, texture) = textures[i];
            var x = start + i * (iconSize + AllyRewardPreviewSettings.IconGap);
            var icon = new TextureRect
            {
                Name = $"Reward{i}",
                // 必须先关闭纹理最小尺寸约束，再设纹理与 Size，避免原图撑大 60px 图标槽。保持原图比例居中缩放。
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Texture = texture, Position = new Vector2(x, 0F), Size = Vector2.One * iconSize,
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(icon);
            if (reward.Amount <= 1M) continue;
            // 添加奖励数量的数字标签
            var amount = CreateLabel(reward.Amount.ToString("0.##", CultureInfo.InvariantCulture),
                AllyRewardPreviewSettings.AmountFontSize, AllyRewardPreviewSettings.AmountOutlineSize);
            amount.Name = "Amount";
            icon.AddChild(amount);
            amount.Size = amount.GetCombinedMinimumSize(); // 获取文本渲染所需的最小真实宽高
            amount.Position = Vector2.One * iconSize + AllyRewardPreviewSettings.AmountOffset - amount.Size * 0.5F;
        }
    }
    
    private static Label CreateLabel(string text, int size, int outline)
    {
        var label = new Label
        {
            Text = text, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore
        };
        if (ResourceLoader.Exists(AllyRewardPreviewSettings.FontPath))
            label.AddThemeFontOverride("font", ResourceLoader.Load<Font>(AllyRewardPreviewSettings.FontPath));
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(1F, 0.964706F, 0.886275F));
        label.AddThemeColorOverride("font_outline_color", new Color(0.2F, 0.2F, 0.2F, 0.9F));
        label.AddThemeConstantOverride("outline_size", outline);
        label.AddThemeColorOverride("font_shadow_color", new Color(0F, 0F, 0F, 0.25F));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        return label;
    }
}