using System.Text.Json.Serialization;

namespace KeyClassify.Models;

/// <summary>
///     Everything the mod reads from config/config.json. Missing properties fall back to the defaults below,
///     so a partial config file is valid.
/// </summary>
public sealed class ModConfig
{
    /// <summary>
    ///     Master switch. When false the handbook is left exactly as SPT shipped it.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Format used for the localized item name. {map} is replaced with the name of the map in the language
    ///     the client requested, {name} with the original name. The {name} placeholder is mandatory.
    /// </summary>
    [JsonPropertyName("nameFormat")]
    public string NameFormat { get; set; } = "【{map}】{name}";

    /// <summary>
    ///     Adds the map name to the top of the item description as well.
    /// </summary>
    [JsonPropertyName("addDescriptionHeader")]
    public bool AddDescriptionHeader { get; set; } = true;

    /// <summary>
    ///     Format of the description header. {map} is replaced with the localized map name.
    /// </summary>
    [JsonPropertyName("descriptionHeader")]
    public string DescriptionHeader { get; set; } = "<color=#00cccc><b>{map}</b></color>";

    /// <summary>
    ///     Keeps keycards (electronic keys) in the same per map groups as normal keys when true. When false
    ///     keycards keep their original handbook category and only normal keys are re-grouped.
    /// </summary>
    [JsonPropertyName("includeKeycards")]
    public bool IncludeKeycards { get; set; } = true;

    /// <summary>
    ///     The keys in keymaps.json that belong to no specific map ("unknown") get a category of their own when
    ///     true. When false they are left untouched in their original category.
    /// </summary>
    [JsonPropertyName("createUnknownCategory")]
    public bool CreateUnknownCategory { get; set; } = true;

    /// <summary>
    ///     Removes the original "mechanical keys" / "electronic keys" categories once nothing is left in them.
    /// </summary>
    [JsonPropertyName("removeEmptiedOriginalCategories")]
    public bool RemoveEmptiedOriginalCategories { get; set; } = true;

    /// <summary>
    ///     Handbook category the map categories are created under. Defaults to the top level "Keys" category.
    /// </summary>
    [JsonPropertyName("parentCategoryId")]
    public string ParentCategoryId { get; set; } = "5b47574386f77428ca22b342";

    /// <summary>
    ///     Handbook category that holds mechanical keys. Used to decide whether an item is a keycard and to
    ///     clean the category up afterwards.
    /// </summary>
    [JsonPropertyName("mechanicalCategoryId")]
    public string MechanicalCategoryId { get; set; } = "5c518ec986f7743b68682ce2";

    /// <summary>
    ///     Handbook category that holds electronic keys (keycards). Used to decide whether an item is a keycard
    ///     and to clean the category up afterwards.
    /// </summary>
    [JsonPropertyName("electronicCategoryId")]
    public string ElectronicCategoryId { get; set; } = "5c518ed586f774119a772aee";

    /// <summary>
    ///     Colour of the generated categories, the game expects an empty string for "no colour".
    /// </summary>
    [JsonPropertyName("categoryColor")]
    public string CategoryColor { get; set; } = string.Empty;
}
