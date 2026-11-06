using Godot;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>统一尺寸的敌人静态预览；没有独立图片时，将原版 Visual 的初始姿态渲染一次。</summary>
public static class EyeInTheSkyPortrait
{
    // 可能的原版敌人待机动画动作的名称列表
    private static readonly string[] SourceArray = ["idle_loop", "idle", "Idle"];
    
    public static void Add(Control cell, MonsterModel monster, float size)
    {
        var name = monster.Id.Entry.ToLowerInvariant();
        // 原版 MonsterModel 没有统一 Portrait 属性。优先使用实际存在的独立图标/全身图；
        // 不把 Spine atlas 当作全身图，也不使用会剧透错误怪物的通用占位图。
        var paths = new[]
        {
            $"res://images/monsters/icons/{name}.png",
            $"res://images/map/placeholder/{name}_boss_icon.png",
            $"res://images/monsters/{name}.png",
            $"res://images/monsters/beta/{name}.png"
        };
        var staticPath = paths.FirstOrDefault(p => ResourceLoader.Exists(p));
        var image = new TextureRect
        {
            Size = Vector2.One * size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        cell.AddChild(image);
        if (staticPath != null)
        {
            image.Texture = ResourceLoader.Load<Texture2D>(staticPath);
            return;
        }
        
        // Service 已按原版图鉴方式创建了仅用于展示的 Creature（NullCombatState）。
        // 不能忽略——幽灵花园鳗的 SetupSkins 会读取 Creature.SlotName。这里仅创建 Visual，不创建 NCreature，不进入真实战斗。
        var viewport = new SubViewport
        {
            Size = Vector2I.One * (int)size,
            TransparentBg = true,
            World2D = new World2D(), Disable3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled
        };
        cell.AddChild(viewport);
        var visuals = monster.CreateVisuals();
        viewport.AddChild(visuals); // _Ready 设置 Bounds/SpineBody，之后才能配置皮肤。
        // ToughEgg 等皮肤读取 Monster.Rng；Service 同时为副本准备了独立外观 RNG。
        visuals.SetUpSkin(monster);
        visuals.UpdatePhobiaMode(monster);
        if (visuals.SpineBody is { } spine && spine.GetSkeleton() is { } skeleton)
        {
            using var data = skeleton.GetData();
            var idle = SourceArray.FirstOrDefault(data.HasAnimation) ?? data.GetAnimationNames().FirstOrDefault();
            using var state = spine.GetAnimationState();
            if (idle != null) state.SetAnimation(idle, loop: false);
            state.SetTimeScale(0);
            state.Update(0);
            state.Apply(skeleton);
            // SpineSprite 的原生 update_skeleton(0) 更新世界骨骼及网格，不推进动画时间。
            spine.BoundObject.Call("update_skeleton", 0f);
        }
        Freeze(visuals);
        var bounds = visuals.Bounds;
        var factor = (size - 12f) / Math.Max(1f, Math.Max(bounds.Size.X, bounds.Size.Y));
        visuals.Scale = Vector2.One * factor;
        visuals.Position = Vector2.One * (size / 2f) - (bounds.Position + bounds.Size / 2f) * factor;
        image.Texture = viewport.GetTexture();
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }
    
    // 递归冻结所有子节点
    private static void Freeze(Node node)
    {
        switch (node)
        {
            case AnimationPlayer animation when animation.CurrentAnimation != "":
                animation.Seek(0, update: true); animation.Pause();
                break;
            case AnimatedSprite2D sprite:
                sprite.Pause(); sprite.Frame = 0;
                break;
        }
        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        foreach (var child in node.GetChildren()) Freeze(child);
    }
}