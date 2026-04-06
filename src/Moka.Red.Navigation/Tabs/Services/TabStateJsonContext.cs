using System.Text.Json.Serialization;

namespace Moka.Red.Navigation.Tabs.Services;

/// <summary>
///     Source-generated JSON serialization context for tab state persistence.
/// </summary>
[JsonSerializable(typeof(TabStateSnapshot))]
[JsonSerializable(typeof(TabSnapshot))]
[JsonSerializable(typeof(GroupSnapshot))]
internal sealed partial class TabStateJsonContext : JsonSerializerContext;
