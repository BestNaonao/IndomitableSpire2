namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", path);
    
    public static string CardImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "card_portraits", path);
    
    public static string BigCardImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "card_portraits", "big", path);
    
    public static string PowerImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "powers", path);
    
    public static string BigPowerImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "powers", "big", path);
    
    public static string RelicImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "relics", path);
    
    public static string BigRelicImagePath(this string path) => 
        Path.Join(MainFile.ModId, "images", "relics", "big", path);
    
    public static string PackedRelicTresPath(this string path) => 
        Path.Join(MainFile.ModId, "images", "relics", "packed", path);
    
    public static string OutlineRelicTresPath(this string path) => 
        Path.Join(MainFile.ModId, "images", "relics", "outline", path);
    
    public static string CharacterUiPath(this string path) => 
        Path.Join(MainFile.ModId, "images", "charui", path);
}