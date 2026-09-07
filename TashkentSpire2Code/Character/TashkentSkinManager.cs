namespace TashkentSpire2.TashkentSpire2Code.Character;

public enum TashkentSkin
{
	Default,
	VariantTwo,
	VariantThree,
	VariantFour
}

public sealed record TashkentSkinDefinition(
	TashkentSkin Id,
	string VisualPath,
	string RestSiteAnimPath,
	string MerchantAnimPath);

public static class TashkentSkinManager
{
	private static readonly IReadOnlyDictionary<TashkentSkin, TashkentSkinDefinition> Skins =
		new Dictionary<TashkentSkin, TashkentSkinDefinition>
		{
			[TashkentSkin.Default] = new(
				TashkentSkin.Default,
				"res://TashkentSpire2/scenes/characters/Tashkent.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_rest_site.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_merchant.tscn"),
			[TashkentSkin.VariantTwo] = new(
				TashkentSkin.VariantTwo,
				"res://TashkentSpire2/scenes/characters/Tashkent_2.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_rest_site_2.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_merchant_2.tscn"),
			[TashkentSkin.VariantThree] = new(
				TashkentSkin.VariantThree,
				"res://TashkentSpire2/scenes/characters/Tashkent_3.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_rest_site_3.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_merchant_3.tscn"),
			[TashkentSkin.VariantFour] = new(
				TashkentSkin.VariantFour,
				"res://TashkentSpire2/scenes/characters/Tashkent_4.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_rest_site_4.tscn",
				"res://TashkentSpire2/scenes/characters/tashkent_merchant_4.tscn")
		};

	public static TashkentSkinDefinition GetDefinition(TashkentSkin skin) => Skins[skin];
}
