using System.Text.Json.Serialization;

namespace KeyClassify.Models;

/// <summary>
///     Shape of config/keymaps.json: every map gets a handbook category and a list of item ids that belong to it.
///     Move an item id between two "keys" arrays to reclassify it, no code change required.
/// </summary>
public sealed class KeyMapFile
{
    private List<KeyMap> _maps = [];

    [JsonPropertyName("maps")]
    public List<KeyMap> Maps
    {
        get => _maps;
        set => _maps = value ?? [];
    }
}

public sealed class KeyMap
{
    private List<string> _keys = [];

    /// <summary>
    ///     Stable identifier of the map, e.g. "customs". Only used in log messages.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Handbook category id. Must be 24 hexadecimal characters and must never change once players have
    ///     profiles, because it is the id written into the handbook tree.
    /// </summary>
    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>
    ///     Sort order inside the parent category, as a numeric string ("101" comes after "100").
    /// </summary>
    [JsonPropertyName("order")]
    public string Order { get; set; } = "100";

    /// <summary>
    ///     Icon shown next to the category. Falls back to the mechanical key icon when empty.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    ///     Locale keys that carry this map's name in the game files, first match wins. "Customs" and "bigmap"
    ///     are two keys for the same map, older and newer name of the locale entry.
    /// </summary>
    [JsonPropertyName("localeKeys")]
    public List<string>? LocaleKeys { get; set; }

    /// <summary>
    ///     Optional per language override for the category / name prefix, e.g. {"ch": "海关"}.
    /// </summary>
    [JsonPropertyName("names")]
    public Dictionary<string, string>? Names { get; set; }

    /// <summary>
    ///     Item template ids of every key / keycard that belongs to this map.
    /// </summary>
    [JsonPropertyName("keys")]
    public List<string> Keys
    {
        get => _keys;
        set => _keys = value ?? [];
    }
}
