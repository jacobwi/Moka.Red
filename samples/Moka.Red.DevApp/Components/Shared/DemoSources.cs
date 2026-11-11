using System.Collections.Concurrent;
using System.Reflection;

namespace Moka.Red.DevApp.Components.Shared;

internal static class DemoSources
{
	private const string Extension = ".razor";

	private static readonly Assembly Self = typeof(DemoSources).Assembly;
	private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Files = new(StringComparer.Ordinal);
	private static readonly Lazy<HashSet<string>> Names = new(() =>
		[.. Self.GetManifestResourceNames().Where(name => name.EndsWith(Extension, StringComparison.Ordinal))]);

	public static string? Find(Type? owner, string id)
	{
		if (owner?.FullName is { } ownerName && SectionsIn(ownerName + Extension) is { } own && own.TryGetValue(id, out string? code))
		{
			return code;
		}

		foreach (string name in Names.Value)
		{
			if (SectionsIn(name) is { } sections && sections.TryGetValue(id, out code))
			{
				return code;
			}
		}

		return null;
	}

	// Razor compiles child content to a lambda on the component that wrote it, or on a closure class nested in it.
	public static Type? OwnerOf(Delegate? fragment)
	{
		Type? type = fragment?.Method.DeclaringType;
		while (type?.DeclaringType is { } outer)
		{
			type = outer;
		}

		return type;
	}

	private static IReadOnlyDictionary<string, string>? SectionsIn(string resource) =>
		Names.Value.Contains(resource) ? Files.GetOrAdd(resource, Load) : null;

	private static IReadOnlyDictionary<string, string> Load(string resource)
	{
		using Stream? stream = Self.GetManifestResourceStream(resource);
		if (stream is null)
		{
			return new Dictionary<string, string>();
		}

		using StreamReader reader = new(stream);
		return RazorSourceSlicer.Slice(reader.ReadToEnd());
	}
}
