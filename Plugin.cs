using System.Reflection;
using KeyClassify.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace KeyClassify;

/// <summary>
///     Mod metadata. The SPT mod loader looks for exactly one type implementing IModMetadata in the assembly.
/// </summary>
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.keyclassify.server";

    public string Name { get; init; } = "KeyClassify";

    public string Author { get; init; } = "ODT";

    public List<string>? Contributors { get; init; }

    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");

    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

    public bool HasPrepatcher { get; init; }

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

    public string? Url { get; init; } = "";

    public string License { get; init; } = "MIT";
}

/// <summary>
///     Runs after every other post-load mod (ItemInfo decorates item names at PostLoad + 2) so the map prefix
///     ends up in front of whatever other mods added, and after the whole database has been assembled but before
///     the first client request touches the handbook.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 50)]
public class KeyClassifyPlugin(
    ISptLogger<KeyClassifyPlugin> logger,
    ModHelper modHelper,
    TemplateTable templateTable,
    LocaleTable localeTable,
    HandbookHelper handbookHelper) : IOnLoad
{
    private const string LogPrefix = "[KeyClassify]";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        string modFolder;
        try
        {
            modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        }
        catch (Exception exception)
        {
            logger.Error($"{LogPrefix} Unable to resolve the folder this mod is running from, the handbook was left untouched.", exception);
            return Task.CompletedTask;
        }

        ModConfig config;
        try
        {
            config = modHelper.GetJsonDataFromFile<ModConfig>(modFolder, "config/config.json");
        }
        catch (Exception exception)
        {
            logger.Warning($"{LogPrefix} config/config.json could not be read ({exception.Message}), using the built in defaults.");
            config = new ModConfig();
        }

        if (!config.Enabled)
        {
            logger.Info($"{LogPrefix} Disabled through config/config.json, the handbook was left untouched.");
            return Task.CompletedTask;
        }

        KeyMapFile keyMaps;
        try
        {
            keyMaps = modHelper.GetJsonDataFromFile<KeyMapFile>(modFolder, "config/keymaps.json");
        }
        catch (Exception exception)
        {
            logger.Error($"{LogPrefix} config/keymaps.json could not be read, no key will be classified.", exception);
            return Task.CompletedTask;
        }

        try
        {
            new KeyClassifier(logger, templateTable, localeTable).Apply(keyMaps, config);
        }
        catch (Exception exception)
        {
            logger.Error($"{LogPrefix} Failed while re-grouping keys, the handbook may be partially modified.", exception);
        }

        // Whatever happened above, the handbook now differs from the copy SPT may have cached while loading.
        HandbookCacheRefresher.Refresh(handbookHelper, logger);

        return Task.CompletedTask;
    }
}
