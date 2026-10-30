using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class ResourceLoaderExtensions
{
    public static Texture2D? LoadTexture(this string path) => 
        ResourceLoader.Exists(path, "Texture2D") ? ResourceLoader.Load<Texture2D>(path) : null;
}