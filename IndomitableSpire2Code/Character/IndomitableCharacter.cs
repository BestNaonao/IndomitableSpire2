using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public sealed class IndomitableDefault : Indomitable
{
    // 现在只需要重写 CurrentSkin，父类就会自动从 SkinManager 中读取子类对应的皮肤定义的各项特色资源
    public override IndomitableSkin CurrentSkin => IndomitableSkin.Default;
}

public sealed class IndomitableMaid : Indomitable
{
    // 现在只需要重写 CurrentSkin，父类就会自动从 SkinManager 中读取子类对应的皮肤定义的各项特色资源
    public override IndomitableSkin CurrentSkin => IndomitableSkin.Maid;
    
    public override bool HideFromVanillaCharacterSelect => true;
    
    public override bool HideInCompendium => true;
    
    public override bool AllowInVanillaRandomCharacterSelect => true;
    
    public override ModelId DefaultCompendiumOpenModelId => ModelDb.Character<IndomitableDefault>().Id;
}

public sealed class IndomitableRaceQueen : Indomitable
{
    // 现在只需要重写 CurrentSkin，父类就会自动从 SkinManager 中读取子类对应的皮肤定义的各项特色资源
    public override IndomitableSkin CurrentSkin => IndomitableSkin.RaceQueen;
    
    public override bool HideFromVanillaCharacterSelect => true;
    
    public override bool HideInCompendium => true;
    
    public override bool AllowInVanillaRandomCharacterSelect => true;
    
    public override ModelId DefaultCompendiumOpenModelId => ModelDb.Character<IndomitableDefault>().Id;
}