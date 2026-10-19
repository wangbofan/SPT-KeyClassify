using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Profile;

namespace KeyClassify;

/// <summary>
///     SPT copies the handbook into a lookup of categories and prices (<see cref="HandbookHelper" />) the first
///     time anything asks for a handbook price or category, and keeps that copy for the lifetime of the server.
///     If another mod (or the server itself while loading profiles) touched it before this mod re-grouped the
///     keys, the flea market would keep filtering by the old category ids and the new map categories would show
///     up empty. Dropping the cache makes the next caller rebuild it from the handbook we just changed.
/// </summary>
internal static class HandbookCacheRefresher
{
    private const string CacheFieldName = "_handbookPriceCache";

    private const string LogPrefix = "[KeyClassify]";

    public static void Refresh(HandbookHelper handbookHelper, ISptLogger<KeyClassifyPlugin> logger)
    {
        try
        {
            var field = typeof(HandbookHelper).GetField(CacheFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is null)
            {
                logger.Warning($"{LogPrefix} SPT changed how it caches the handbook ('{CacheFieldName}' was not found). The handbook itself is fine, but the flea market filter may still use the categories from before this mod ran.");
                return;
            }

            var wasBuilt = field.GetValue(handbookHelper) is not null;
            field.SetValue(handbookHelper, null);

            if (wasBuilt)
            {
                logger.Info($"{LogPrefix} Dropped SPT's cached handbook lookup so the flea market rebuilds it with the map categories.");
            }
        }
        catch (Exception exception)
        {
            logger.Warning($"{LogPrefix} Could not reset SPT's cached handbook lookup ({exception.Message}). The handbook itself is fine, but the flea market filter may still use the categories from before this mod ran.");
        }
    }
}
