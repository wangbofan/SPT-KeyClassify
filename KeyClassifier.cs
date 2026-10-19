using KeyClassify.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace KeyClassify;

/// <summary>
///     Re-parents every configured key / keycard into a handbook category that represents the map it opens,
///     and decorates the localized item name / description with that map.
///     <para>
///         The flea market renders the same handbook tree and SPT filters flea offers by handbook id
///         (RagfairHelper.GetCategoryList), so a key that moves into "Customs" in the handbook also appears
///         under "Customs" in the flea market.
///     </para>
/// </summary>
public sealed class KeyClassifier(
    ISptLogger<KeyClassifyPlugin> logger,
    TemplateTable templateTable,
    LocaleTable localeTable)
{
    private const string LogPrefix = "[KeyClassify]";

    private const string KeysCategoryId = "5b47574386f77428ca22b342";

    private const string DefaultIcon = "/files/handbook/icon_keys_mechanic.png";

    private const string FallbackNameFormat = "【{map}】{name}";

    private const string FallbackDescriptionHeader = "<color=#00cccc><b>{map}</b></color>";

    private const string NamePlaceholder = "{name}";

    private const string MapPlaceholder = "{map}";

    private const string UnknownMapId = "unknown";

    public void Apply(KeyMapFile keyMapFile, ModConfig config)
    {
        var handbook = templateTable.Handbook;
        if (handbook?.Categories is null || handbook.Items is null)
        {
            logger.Error($"{LogPrefix} The handbook table is unavailable, no key will be classified.");
            return;
        }

        var nameFormat = NormalizeFormat(config.NameFormat, FallbackNameFormat, NamePlaceholder, "nameFormat");
        var descriptionHeader = NormalizeFormat(config.DescriptionHeader, FallbackDescriptionHeader, MapPlaceholder, "descriptionHeader");
        var parentCategoryId = NormalizeCategoryId(config.ParentCategoryId) ?? KeysCategoryId;

        var categories = handbook.Categories;
        var items = handbook.Items;

        var categoriesById = new Dictionary<string, HandbookCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            if (category?.Id is null)
            {
                continue;
            }

            categoriesById[category.Id.ToString()] = category;
        }

        if (!categoriesById.ContainsKey(parentCategoryId))
        {
            logger.Warning($"{LogPrefix} The configured parent category '{parentCategoryId}' does not exist in the handbook, using '{KeysCategoryId}' instead.");
            parentCategoryId = KeysCategoryId;
        }

        var maps = SelectMaps(keyMapFile);
        if (maps.Count == 0)
        {
            logger.Warning($"{LogPrefix} config/keymaps.json does not contain a single usable map, nothing to do.");
            return;
        }

        var mapByItem = MapItemsToMaps(maps, config);

        var assignments = new List<(HandbookItem Item, KeyMap Map)>();
        var seenItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedKeycards = 0;

        foreach (var item in items)
        {
            var templateId = item.Id.ToString();
            if (!mapByItem.TryGetValue(templateId, out var map))
            {
                continue;
            }

            seenItems.Add(templateId);

            if (!config.IncludeKeycards && IsKeycard(item, config))
            {
                skippedKeycards++;
                continue;
            }

            assignments.Add((item, map));
        }

        var missing = mapByItem.Keys.Where(templateId => !seenItems.Contains(templateId)).ToList();

        if (assignments.Count == 0)
        {
            logger.Warning($"{LogPrefix} None of the configured keys exist in the handbook, nothing to do.");
            return;
        }

        var created = 0;
        var refreshed = 0;
        var usedMaps = new List<KeyMap>();

        foreach (var group in assignments.GroupBy(assignment => assignment.Map))
        {
            var map = group.Key;
            usedMaps.Add(map);

            var icon = string.IsNullOrWhiteSpace(map.Icon) ? DefaultIcon : map.Icon;
            var order = string.IsNullOrWhiteSpace(map.Order) ? "100" : map.Order;

            if (categoriesById.TryGetValue(map.CategoryId, out var existing))
            {
                existing.ParentId = new MongoId(parentCategoryId);
                existing.Icon = icon;
                existing.Order = order;
                if (!string.IsNullOrEmpty(config.CategoryColor))
                {
                    existing.Color = config.CategoryColor;
                }

                refreshed++;
                continue;
            }

            var category = new HandbookCategory
            {
                Id = new MongoId(map.CategoryId),
                ParentId = new MongoId(parentCategoryId),
                Icon = icon,
                Color = config.CategoryColor ?? string.Empty,
                Order = order,
            };

            categories.Add(category);
            categoriesById[map.CategoryId] = category;
            created++;
        }

        var moved = 0;
        foreach (var (item, map) in assignments)
        {
            if (string.Equals(item.ParentId.ToString(), map.CategoryId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            item.ParentId = new MongoId(map.CategoryId);
            moved++;
        }

        var removed = 0;
        if (config.RemoveEmptiedOriginalCategories)
        {
            foreach (var sourceCategoryId in new[] { NormalizeCategoryId(config.MechanicalCategoryId), NormalizeCategoryId(config.ElectronicCategoryId) })
            {
                if (sourceCategoryId is null || string.Equals(sourceCategoryId, parentCategoryId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!categoriesById.TryGetValue(sourceCategoryId, out var sourceCategory))
                {
                    continue;
                }

                if (items.Any(item => string.Equals(item.ParentId.ToString(), sourceCategoryId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                categories.Remove(sourceCategory);
                categoriesById.Remove(sourceCategoryId);
                removed++;
            }
        }

        AddLocaleDecorations(usedMaps, assignments, nameFormat, descriptionHeader, config.AddDescriptionHeader);

        logger.Info($"{LogPrefix} Classified {assignments.Count} key(s) into {usedMaps.Count} map categories ({created} created, {refreshed} refreshed, {moved} re-parented, {removed} emptied category/categories removed).");

        if (skippedKeycards > 0)
        {
            logger.Info($"{LogPrefix} {skippedKeycards} keycard(s) stayed where they were because includeKeycards is disabled.");
        }

        if (missing.Count > 0)
        {
            logger.Debug($"{LogPrefix} {missing.Count} configured item id(s) are not present in the handbook: {string.Join(", ", missing.Take(10))}{(missing.Count > 10 ? ", ..." : string.Empty)}");
        }
    }

    /// <summary>
    ///     Every game language gets one transformer that is evaluated the first time that language is requested.
    ///     Doing the work lazily keeps startup fast (locales are ~1 MB per language) and lets us read the bare
    ///     name of the current language instead of guessing at translation strings.
    /// </summary>
    private void AddLocaleDecorations(
        List<KeyMap> maps,
        List<(HandbookItem Item, KeyMap Map)> assignments,
        string nameFormat,
        string descriptionHeader,
        bool addDescriptionHeader)
    {
        foreach (var (languageCode, localeLoader) in localeTable.Global)
        {
            if (localeLoader is null)
            {
                continue;
            }

            localeLoader.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return null;
                }

                var mapNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var map in maps)
                {
                    mapNames[map.Id] = ResolveMapName(map, localeData, languageCode);
                    localeData[map.CategoryId] = mapNames[map.Id];
                }

                foreach (var (item, map) in assignments)
                {
                    var templateId = item.Id.ToString();
                    var mapName = mapNames[map.Id];

                    ApplyName(localeData, templateId, mapName, nameFormat);

                    if (addDescriptionHeader)
                    {
                        ApplyDescription(localeData, templateId, mapName, descriptionHeader);
                    }
                }

                return localeData;
            });
        }
    }

    private List<KeyMap> SelectMaps(KeyMapFile keyMapFile)
    {
        var maps = new List<KeyMap>();
        var categoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in keyMapFile.Maps)
        {
            if (map is null || string.IsNullOrWhiteSpace(map.Id))
            {
                logger.Error($"{LogPrefix} config/keymaps.json contains a map without an id, it is skipped.");
                continue;
            }

            var categoryId = NormalizeCategoryId(map.CategoryId);
            if (categoryId is null)
            {
                logger.Error($"{LogPrefix} Map '{map.Id}' has the categoryId '{map.CategoryId}'. A handbook category id must be exactly 24 hexadecimal characters, this map is skipped.");
                continue;
            }

            if (!categoryIds.Add(categoryId))
            {
                logger.Error($"{LogPrefix} Map '{map.Id}' reuses the categoryId '{categoryId}' of an earlier map, this map is skipped.");
                continue;
            }

            map.CategoryId = categoryId;
            maps.Add(map);
        }

        return maps;
    }

    private Dictionary<string, KeyMap> MapItemsToMaps(List<KeyMap> maps, ModConfig config)
    {
        var mapByItem = new Dictionary<string, KeyMap>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in maps)
        {
            if (!config.CreateUnknownCategory && string.Equals(map.Id, UnknownMapId, StringComparison.OrdinalIgnoreCase))
            {
                logger.Info($"{LogPrefix} Map '{map.Id}' is disabled through createUnknownCategory, its {map.Keys.Count} item(s) stay where they are.");
                continue;
            }

            foreach (var templateId in map.Keys)
            {
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    continue;
                }

                var itemId = templateId.Trim();
                if (!mapByItem.TryAdd(itemId, map))
                {
                    logger.Warning($"{LogPrefix} Item '{itemId}' is listed for both '{mapByItem[itemId].Id}' and '{map.Id}', it stays in '{mapByItem[itemId].Id}'.");
                }
            }
        }

        return mapByItem;
    }

    private static bool IsKeycard(HandbookItem item, ModConfig config)
    {
        var electronicCategoryId = NormalizeCategoryId(config.ElectronicCategoryId);
        return electronicCategoryId is not null && string.Equals(item.ParentId.ToString(), electronicCategoryId, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveMapName(KeyMap map, Dictionary<string, string> localeData, string languageCode)
    {
        if (map.Names is not null && map.Names.TryGetValue(languageCode, out var configuredName) && !string.IsNullOrWhiteSpace(configuredName))
        {
            return configuredName;
        }

        if (map.LocaleKeys is { Count: > 0 })
        {
            foreach (var localeKey in map.LocaleKeys)
            {
                if (string.IsNullOrWhiteSpace(localeKey))
                {
                    continue;
                }

                if (localeData.TryGetValue(localeKey, out var localeName) && !string.IsNullOrWhiteSpace(localeName))
                {
                    return localeName;
                }
            }
        }

        if (map.Names is not null && map.Names.TryGetValue("en", out var englishName) && !string.IsNullOrWhiteSpace(englishName))
        {
            return englishName;
        }

        return map.Id;
    }

    private static void ApplyName(Dictionary<string, string> localeData, string templateId, string mapName, string nameFormat)
    {
        var localeKey = templateId + " Name";
        if (!localeData.TryGetValue(localeKey, out var currentName) || string.IsNullOrWhiteSpace(currentName))
        {
            return;
        }

        var rendered = nameFormat.Replace(MapPlaceholder, mapName, StringComparison.Ordinal);
        var nameIndex = rendered.IndexOf(NamePlaceholder, StringComparison.Ordinal);
        var prefix = nameIndex >= 0 ? rendered[..nameIndex] : rendered;
        var suffix = nameIndex >= 0 ? rendered[(nameIndex + NamePlaceholder.Length)..] : string.Empty;

        // Already decorated (another mod reloaded locales without re-reading them, or the user re-ran the mod).
        if (prefix.Length > 0 && currentName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        localeData[localeKey] = prefix + currentName + suffix;
    }

    private static void ApplyDescription(Dictionary<string, string> localeData, string templateId, string mapName, string descriptionFormat)
    {
        var localeKey = templateId + " Description";
        if (!localeData.TryGetValue(localeKey, out var currentDescription) || string.IsNullOrWhiteSpace(currentDescription))
        {
            return;
        }

        var header = descriptionFormat.Replace(MapPlaceholder, mapName, StringComparison.Ordinal);
        if (currentDescription.StartsWith(header, StringComparison.Ordinal))
        {
            return;
        }

        localeData[localeKey] = header + "\n" + currentDescription;
    }

    private string NormalizeFormat(string? value, string fallback, string requiredPlaceholder, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (value.Contains(requiredPlaceholder, StringComparison.Ordinal))
        {
            return value;
        }

        logger.Warning($"{LogPrefix} '{settingName}' in config.json has no {requiredPlaceholder} placeholder, falling back to '{fallback}'.");
        return fallback;
    }

    private static string? NormalizeCategoryId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length != 24)
        {
            return null;
        }

        foreach (var character in trimmed)
        {
            if (!Uri.IsHexDigit(character))
            {
                return null;
            }
        }

        return trimmed.ToLowerInvariant();
    }
}
