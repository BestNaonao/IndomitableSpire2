using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentVariantTwo : TashkentCharacter
{
	public override TashkentSkin CurrentSkin => TashkentSkin.VariantTwo;
	public override bool HideFromVanillaCharacterSelect => true;
	public override bool HideInCompendium => true;
	public override bool AllowInVanillaRandomCharacterSelect => false;
	public override ModelId DefaultCompendiumOpenModelId => ModelDb.Character<TashkentCharacter>().Id;
}

public sealed class TashkentVariantThree : TashkentCharacter
{
	public override TashkentSkin CurrentSkin => TashkentSkin.VariantThree;
	public override bool HideFromVanillaCharacterSelect => true;
	public override bool HideInCompendium => true;
	public override bool AllowInVanillaRandomCharacterSelect => false;
	public override ModelId DefaultCompendiumOpenModelId => ModelDb.Character<TashkentCharacter>().Id;
}

public sealed class TashkentVariantFour : TashkentCharacter
{
	public override TashkentSkin CurrentSkin => TashkentSkin.VariantFour;
	public override bool HideFromVanillaCharacterSelect => true;
	public override bool HideInCompendium => true;
	public override bool AllowInVanillaRandomCharacterSelect => false;
	public override ModelId DefaultCompendiumOpenModelId => ModelDb.Character<TashkentCharacter>().Id;
}
