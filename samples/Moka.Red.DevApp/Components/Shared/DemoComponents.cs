using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

[assembly: SuppressMessage("Naming", "CA1716", Scope = "namespace", Target = "~N:Moka.Red.DevApp.Components.Shared",
	Justification = "The folder's name was asked for, and the dev app is never called from Visual Basic.")]

namespace Moka.Red.DevApp.Components.Shared;

internal static class DemoComponents
{
	private const string LibraryPrefix = "Moka.Red";

	private static readonly Lazy<Dictionary<string, Type>> Types = new(Load);

	public static Type? Find(string name) => Types.Value.GetValueOrDefault(name);

	public static string DisplayName(Type type)
	{
		string name = WithoutArity(type.Name);
		return type.IsGenericTypeDefinition
			? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(argument => argument.Name))}>"
			: name;
	}

	private static Dictionary<string, Type> Load()
	{
		Dictionary<string, Type> types = new(StringComparer.Ordinal);
		foreach (Assembly assembly in LibraryAssemblies())
		{
			foreach (Type type in assembly.GetExportedTypes())
			{
				if (!type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
				{
					types.TryAdd(WithoutArity(type.Name), type);
				}
			}
		}

		return types;
	}

	// Walks the references from the dev app, because an assembly no page has touched yet isn't loaded.
	private static IEnumerable<Assembly> LibraryAssemblies()
	{
		HashSet<string> seen = new(StringComparer.Ordinal);
		Queue<Assembly> pending = new([typeof(DemoComponents).Assembly]);
		while (pending.TryDequeue(out Assembly? assembly))
		{
			foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
			{
				if (reference.Name is { } name && name.StartsWith(LibraryPrefix, StringComparison.Ordinal) && seen.Add(name))
				{
					Assembly loaded = Assembly.Load(reference);
					pending.Enqueue(loaded);
					yield return loaded;
				}
			}
		}
	}

	private static string WithoutArity(string name)
	{
		int tick = name.IndexOf('`', StringComparison.Ordinal);
		return tick < 0 ? name : name[..tick];
	}
}
