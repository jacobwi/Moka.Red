using System.Collections.Concurrent;
using System.Reflection;

namespace Moka.Red.Showcase.Components.Shared;

// The project embeds its own demo files, so a section's Code tab shows exactly what renders it.
internal static class DemoSources
{
	private const string Extension = ".razor";

	private static readonly Assembly Self = typeof(DemoSources).Assembly;
	private static readonly ConcurrentDictionary<string, DemoFile> Files = new(StringComparer.Ordinal);
	private static readonly DemoFile Empty = new(new Dictionary<string, string>(), []);
	private static readonly Lazy<HashSet<string>> Names = new(() =>
		[.. Self.GetManifestResourceNames().Where(name => name.EndsWith(Extension, StringComparison.Ordinal))]);

	public static string? Find(Type? owner, string id)
	{
		if (owner is not null && FileOf(owner).Sections.TryGetValue(id, out string? code))
		{
			return code;
		}

		foreach (string name in Names.Value)
		{
			if (Load(name).Sections.TryGetValue(id, out code))
			{
				return code;
			}
		}

		return null;
	}

	public static IReadOnlyList<DemoOutlineEntry> Outline(Type demos) => FileOf(demos).Outline;

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

	private static DemoFile FileOf(Type type) =>
		type.FullName is { } name && Names.Value.Contains(name + Extension) ? Load(name + Extension) : Empty;

	private static DemoFile Load(string resource) => Files.GetOrAdd(resource, Read);

	private static DemoFile Read(string resource)
	{
		using Stream? stream = Self.GetManifestResourceStream(resource);
		if (stream is null)
		{
			return Empty;
		}

		using StreamReader reader = new(stream);
		return RazorSourceSlicer.Slice(reader.ReadToEnd());
	}
}
