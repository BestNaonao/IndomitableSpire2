using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>A bounded cosmetic shot for the Architect's VisualOnly room, not an AttackCommand.</summary>
public partial class NTashkentArchitectShotVfx : Node2D
{
    private const float ShotSpacing = 0.2f;
    private const float FlightTime = 0.14f;
    private NCombatRoom _room = null!;
    private NCreature _attacker = null!, _target = null!;
    private Sprite2D _flash = null!, _flame = null!, _projectile = null!, _smoke = null!;
    private int _shotCount, _fired;
    private float _elapsed, _age;
    private bool _impacted;
    private Vector2 _muzzle, _hit;

    public static bool Play(NCombatRoom room, Creature attacker, int shots)
    {
        if (room.Mode != CombatRoomMode.VisualOnly || !room.IsInsideTree() || room.IsQueuedForDeletion()
            || room.GetCreatureNode(attacker) is not { } source || !source.IsInsideTree())
            return false;
        NCreature? target = room.CreatureNodes.FirstOrDefault(node => node.Entity.Monster is Architect);
        if (target == null || !target.IsInsideTree()) return false;
        // There is only one cue in a conversation. Bound live effects even if a
        // third-party patch calls the presentation hook repeatedly.
        if (room.CombatVfxContainer.GetChildren().OfType<NTashkentArchitectShotVfx>()
            .Any(node => !node.IsQueuedForDeletion())) return false;
        room.CombatVfxContainer.AddChildSafely(new NTashkentArchitectShotVfx
        {
            Name = "TashkentArchitectShot", _room = room, _attacker = source,
            _target = target, _shotCount = Math.Clamp(shots, 1, 3)
        });
        return true;
    }

    internal static void StopForRoom(NCombatRoom? room)
    {
        if (!GodotObject.IsInstanceValid(room)) return;
        foreach (var effect in room!.CombatVfxContainer.GetChildren().OfType<NTashkentArchitectShotVfx>())
        {
            effect.Hide(); effect.SetProcess(false); effect.QueueFreeSafely();
        }
    }

    public override void _Ready()
    {
        var additive = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        Texture2D flash = PreloadManager.Cache.GetTexture2D(NAmmunitionMuzzleVfx.FlashPath);
        _flash = new Sprite2D { Texture = flash, Material = additive };
        _flame = new Sprite2D { Texture = PreloadManager.Cache.GetTexture2D(NAmmunitionMuzzleVfx.FlamePath),
            Material = additive, FlipH = true };
        _projectile = new Sprite2D { Texture = flash, Material = additive, FlipH = true };
        _smoke = new Sprite2D { Texture = PreloadManager.Cache.GetTexture2D(NAmmunitionMuzzleVfx.SmokePath) };
        AddChild(_smoke); AddChild(_flame); AddChild(_flash); AddChild(_projectile);
        Fire();
        UpdateSprites();
    }

    private void Fire()
    {
        _hit = _target.VfxSpawnPosition;
        Vector2 direction = (_hit - _attacker.VfxSpawnPosition).Normalized();
        _muzzle = _attacker.VfxSpawnPosition + direction * 100f;
        _age = 0f; _impacted = false;
        _attacker.SetAnimationTrigger("Attack"); // Static portraits safely have no animator.
        if (_fired == 0 && _attacker.Entity.Player is { } player)
            SfxCmd.Play(player.Character.AttackSfx);
        _fired++;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_room) || _room != NCombatRoom.Instance ||
            !_room.IsInsideTree() || _room.IsQueuedForDeletion() ||
            !GodotObject.IsInstanceValid(_attacker) || !_attacker.IsInsideTree() ||
            !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree())
        {
            Hide(); this.QueueFreeSafely(); return;
        }
        float dt = Math.Max(0f, (float)delta);
        _elapsed += dt; _age += dt;
        if (!_impacted && _age >= FlightTime)
        {
            _impacted = true;
            VfxCmd.PlayOnCreatureCenter(_target.Entity, "vfx/vfx_fire_smoke_puff");
            _target.SetAnimationTrigger("Hit");
        }
        if (_fired < _shotCount && _elapsed >= _fired * ShotSpacing) Fire();
        UpdateSprites();
        if (_fired == _shotCount && _age >= 0.6f) this.QueueFreeSafely();
    }

    private void UpdateSprites()
    {
        float angle = (_hit - _muzzle).Angle();
        _flash.GlobalPosition = _muzzle; _flash.GlobalRotation = angle;
        _flash.Scale = new Vector2(150f, 105f) / _flash.Texture.GetSize();
        _flash.Modulate = new Color(1f, .92f, .72f, Mathf.Clamp(1f - _age / .09f, 0f, 1f));
        _flame.GlobalPosition = _muzzle + Vector2.FromAngle(angle) * 50f; _flame.GlobalRotation = angle;
        _flame.Scale = new Vector2(210f, 80f) / _flame.Texture.GetSize();
        _flame.Modulate = new Color(1f, .85f, .55f, Mathf.Clamp(1f - _age / .13f, 0f, 1f));
        _projectile.Visible = _age < FlightTime;
        _projectile.GlobalPosition = _muzzle.Lerp(_hit, Mathf.Clamp(_age / FlightTime, 0f, 1f));
        _projectile.GlobalRotation = angle;
        _projectile.Scale = new Vector2(55f, 18f) / _projectile.Texture.GetSize();
        _smoke.GlobalPosition = _muzzle + new Vector2(0f, -_age * 35f);
        _smoke.Scale = Vector2.One * ((40f + _age * 90f) / _smoke.Texture.GetWidth());
        _smoke.Modulate = new Color(.78f, .8f, .83f,
            Mathf.Clamp(_age / .05f, 0f, 1f) * Mathf.Clamp(1f - _age / .55f, 0f, 1f) * .4f);
    }
}
