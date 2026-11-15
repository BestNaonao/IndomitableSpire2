using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace TashkentSpire2.Launcher;

[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "TashkentSpire2";
    private const string ImplementationName = ModId + ".Impl";
    private const string BaseResourcesVariant = "g0.111.0";
    private static int _started;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException($"{ModId} was already initialized.");

        var launcher = typeof(Bootstrap).Assembly;
        string root = Path.GetDirectoryName(launcher.Location)
            ?? throw new InvalidOperationException($"Cannot resolve the {ModId} launcher directory.");
        Version? hostVersion = ReadGameVersion(OS.GetExecutablePath());
        string variant = SelectVariant(Path.Combine(root, "bin"), hostVersion);
        Log.Info($"[{ModId}] Game {hostVersion?.ToString() ?? "unknown"}, loading {Path.GetFileName(variant)}.");

        string dll = Path.Combine(variant, ImplementationName + ".dll");
        var context = AssemblyLoadContext.GetLoadContext(launcher) ?? AssemblyLoadContext.Default;
        var implementation = context.LoadFromAssemblyPath(dll);
        if (implementation.GetName().Name != ImplementationName || implementation.GetName().Version != launcher.GetName().Version)
            throw new InvalidOperationException($"[{ModId}] Unexpected implementation assembly {implementation.FullName} in {dll}.");
        var entryPoint = implementation.GetType($"{ModId}.{ModId}Code.MainFile", throwOnError: true)!
            .GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)
            ?? throw new MissingMethodException(implementation.FullName, "MainFile.Initialize");
        if (entryPoint.ReturnType != typeof(void))
            throw new InvalidOperationException($"[{ModId}] MainFile.Initialize must return void.");

        foreach (string pack in GetPackPaths(variant))
        {
            Log.Info($"[{ModId}] Mounting {pack}.");
            if (!ProjectSettings.LoadResourcePack(pack, replaceFiles: true))
                throw new InvalidOperationException($"[{ModId}] Godot failed to mount {pack}.");
        }

        ModManager.AssociateAssemblyWithMod(ModId, implementation);
        if (!ModManager.Mods.Any(mod => mod.manifest?.id == ModId && mod.assemblies.Contains(implementation)))
            throw new InvalidOperationException($"[{ModId}] The game did not register {ImplementationName}.");

        try
        {
            entryPoint.Invoke(null, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private static Version? ReadGameVersion(string executable)
    {
        string? directory = Path.GetDirectoryName(executable);
        if (directory == null)
            return null;

        foreach (string path in new[]
        {
            Path.Combine(directory, "release_info.json"),
            Path.Combine(directory, "..", "Resources", "release_info.json")
        })
        {
            if (!File.Exists(path))
                continue;
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                if (json.RootElement.TryGetProperty("version", out var property)
                    && property.ValueKind == JsonValueKind.String
                    && Version.TryParse(property.GetString()?.TrimStart('v', 'V'), out var version))
                    return version;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or JsonException or InvalidOperationException)
            {
                Log.Warn($"[{ModId}] Could not read {path}: {exception.Message}");
            }
        }
        return null;
    }

    internal static string SelectVariant(string directory, Version? hostVersion)
    {
        var candidates = Directory.Exists(directory)
            ? Directory.EnumerateDirectories(directory, "g*")
                .Select(path => (Path: path, Version: Version.TryParse(Path.GetFileName(path).AsSpan(1), out var v) ? v : null))
                .Where(candidate => candidate.Version != null)
                .OrderByDescending(candidate => candidate.Version)
                .ToArray()
            : [];

        var match = hostVersion == null ? default
            : candidates.FirstOrDefault(candidate => candidate.Version! <= hostVersion);
        if (match.Path != null)
            return RequirePair(match.Path);

        string latest = Path.Combine(directory, "latest");
        if (Directory.Exists(latest))
            return RequirePair(latest);
        if (hostVersion == null && candidates.Length > 0)
            return RequirePair(candidates[0].Path);

        throw new InvalidOperationException($"[{ModId}] No implementation for game {hostVersion?.ToString() ?? "unknown"} in {directory}.");
    }

    private static string RequirePair(string directory)
    {
        if (!File.Exists(Path.Combine(directory, ImplementationName + ".dll")))
            throw new InvalidOperationException($"[{ModId}] Incomplete implementation (missing DLL): {directory}.");
        _ = GetPackPaths(directory);
        return directory;
    }

    private static string[] GetPackPaths(string directory)
    {
        string patch = Path.Combine(directory, ModId + ".patch.pck");
        if (File.Exists(patch))
        {
            string basePack = Path.GetFullPath(Path.Combine(directory, "..", BaseResourcesVariant, ModId + ".pck"));
            if (!File.Exists(basePack))
                throw new InvalidOperationException($"[{ModId}] Missing base resource pack for {patch}: {basePack}.");

            return [basePack, patch];
        }

        string fullPack = Path.Combine(directory, ModId + ".pck");
        if (File.Exists(fullPack))
            return [fullPack];

        throw new InvalidOperationException($"[{ModId}] Incomplete implementation (missing PCK or PCK patch): {directory}.");
    }
}
