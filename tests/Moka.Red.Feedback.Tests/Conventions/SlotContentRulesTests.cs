using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     What an app passes in (ChildContent, a template, any slot) carries the app's scope attribute, not the component's,
///     and CSS isolation puts the component's attribute on the last part of every selector. So a scoped rule that reaches
///     into an element holding nothing but a slot never matches: <c>.moka-dialog-body--no-scroll &gt; :only-child</c>
///     compiled to <c>:only-child[b-dialog]</c>, and the app's one child never filled the body. Such a rule needs
///     <c>::deep</c> after the wrapper (<c>components/css-isolation.md</c>: <c>div ::deep &gt; a</c> becomes
///     <c>div[b-xyz] &gt; a</c>).
/// </summary>
/// <remarks>
///     A wrapper is an element whose whole content is one slot, <c>&lt;div class="x"&gt;@Media&lt;/div&gt;</c>, so anything a
///     rule finds inside it is the app's. Classes come from the markup or from a CssBuilder property used only on such
///     wrappers. An element that holds a slot beside markup of its own isn't counted, since a rule into it may mean its own
///     elements; nor is a class passed to a child component, which styles that component's element, not app content.
/// </remarks>
public sealed partial class SlotContentRulesTests
{
	private static readonly string[] DevTools = ["Moka.Red.Diagnostics"];

	[Fact]
	public void NoScopedRule_ReachesIntoAnElementThatHoldsOnlyASlot()
	{
		List<string> found = [];
		foreach (Component component in Components())
		{
			if (component.Wrappers.Count == 0)
			{
				continue;
			}

			string css = Comment().Replace(component.Css.ReplaceLineEndings("\n"), " ");
			foreach (Match rule in RuleHead().Matches(css))
			{
				string head = rule.Groups[1].Value.Trim();
				if (head.StartsWith('@') || head is "from" or "to" || head.EndsWith('%'))
				{
					continue;
				}

				found.AddRange(TopLevelSelectors(head)
					.Where(selector => !selector.Contains("::deep", StringComparison.Ordinal) && ReachesInto(selector, component.Wrappers))
					.Select(selector => $"{component.File}: {selector}"));
			}
		}

		Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
	}

	// The check reads markup with patterns. If they stopped matching, the test above would pass while checking nothing.
	[Theory]
	[InlineData("MokaDialog.razor.css", "moka-dialog-body--no-scroll")]
	[InlineData("MokaDonutChart.razor.css", "moka-donutchart-center")]
	[InlineData("MokaCard.razor.css", "moka-card-media")]
	[InlineData("MokaTable.razor.css", "moka-table-footer")]
	public void TheCheck_FindsTheElementsThatHoldOnlyASlot(string stylesheet, string wrapper)
	{
		Component component = Components().Single(c => Path.GetFileName(c.File) == stylesheet);

		Assert.Contains(wrapper, component.Wrappers);
	}

	private static bool ReachesInto(string selector, HashSet<string> wrappers)
	{
		List<string> parts = Parts(selector);
		for (int i = 0; i < parts.Count - 1; i++)
		{
			if (parts[i] is ">" or "+" or "~")
			{
				continue;
			}

			// A sibling combinator moves beside the wrapper, not into it.
			if (parts[i + 1] is "+" or "~")
			{
				continue;
			}

			string outside = Arguments().Replace(parts[i], string.Empty);
			if (ClassName().Matches(outside).Any(m => wrappers.Contains(m.Groups[1].Value)))
			{
				return true;
			}
		}

		return false;
	}

	// Compound selectors and the combinators between them, at the top level. A space inside :is() or [ ] is not one.
	private static List<string> Parts(string selector)
	{
		List<string> parts = [];
		StringBuilder current = new();
		int depth = 0;
		foreach (char c in selector)
		{
			if (c is '(' or '[')
			{
				depth++;
			}
			else if (c is ')' or ']')
			{
				depth--;
			}

			if (depth == 0 && c is ' ' or '>' or '+' or '~' or '\n' or '\t')
			{
				if (current.Length > 0)
				{
					parts.Add(current.ToString());
					current.Clear();
				}

				if (c is '>' or '+' or '~')
				{
					parts.Add(c.ToString());
				}
			}
			else
			{
				current.Append(c);
			}
		}

		if (current.Length > 0)
		{
			parts.Add(current.ToString());
		}

		return parts;
	}

	// A comma inside :not() or :is() does not separate selectors.
	private static IEnumerable<string> TopLevelSelectors(string head)
	{
		int depth = 0, start = 0;
		for (int i = 0; i < head.Length; i++)
		{
			switch (head[i])
			{
				case '(':
					depth++;
					break;
				case ')':
					depth--;
					break;
				case ',' when depth == 0:
					yield return head[start..i].Trim();
					start = i + 1;
					break;
			}
		}

		yield return head[start..].Trim();
	}

	private static IEnumerable<Component> Components()
	{
		string src = FindSrc();
		foreach (string css in Directory.EnumerateFiles(src, "*.razor.css", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, css);
			string razor = css[..^".css".Length];
			if (DevTools.Any(tool => relative.StartsWith(tool, StringComparison.Ordinal)) || !File.Exists(razor) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
			    relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			{
				continue;
			}

			string markup = File.ReadAllText(razor).ReplaceLineEndings("\n");
			string stem = Path.GetFileName(razor)[..^".razor".Length];
			StringBuilder code = new(markup);
			foreach (string partial in Directory.EnumerateFiles(Path.GetDirectoryName(razor)!, $"{stem}.*.cs"))
			{
				code.Append('\n').Append(File.ReadAllText(partial).ReplaceLineEndings("\n"));
			}

			yield return new Component(relative, File.ReadAllText(css), Wrappers(markup, code.ToString()));
		}
	}

	private static HashSet<string> Wrappers(string markup, string code)
	{
		HashSet<string> slots = [.. Slot().Matches(code).Select(m => m.Groups[1].Value)];
		HashSet<string> wrappers = new(StringComparer.Ordinal);
		Dictionary<string, List<bool>> builders = new(StringComparer.Ordinal);
		foreach (Match element in Element().Matches(markup))
		{
			Match content = OnlyASlot().Match(markup, element.Index + element.Length);
			bool onlyASlot = content.Success && content.Index == element.Index + element.Length &&
			                 slots.Contains(content.Groups[1].Value) &&
			                 content.Groups[2].Value == element.Groups[1].Value;
			foreach (string name in element.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				if (name.StartsWith('@'))
				{
					string property = name.TrimStart('@').Trim('(', ')');
					if (!builders.TryGetValue(property, out List<bool>? uses))
					{
						builders[property] = uses = [];
					}

					uses.Add(onlyASlot);
				}
				else if (onlyASlot)
				{
					wrappers.Add(name);
				}
			}
		}

		string? root = RootClass().Match(code) is { Success: true } rootMatch ? rootMatch.Groups[1].Value : null;
		foreach ((string property, List<bool> uses) in builders)
		{
			if (!uses.TrueForAll(onlyASlot => onlyASlot))
			{
				continue;
			}

			Match builder = Regex.Match(code, $@"\b{Regex.Escape(property)}\s*=>\s*new CssBuilder\(([^)]*)\)(.*?)\.Build\(\)",
				RegexOptions.Singleline);
			if (!builder.Success)
			{
				continue;
			}

			wrappers.UnionWith(Quoted().Matches(builder.Groups[1].Value).Select(m => m.Groups[1].Value));
			wrappers.UnionWith(Added().Matches(builder.Groups[2].Value).Select(m => m.Groups[1].Value));
			if (root is not null && builder.Groups[1].Value.Contains("RootClass", StringComparison.Ordinal))
			{
				wrappers.Add(root);
			}
		}

		return wrappers;
	}

	private static string FindSrc()
	{
		for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
		{
			string candidate = Path.Combine(dir.FullName, "src");
			if (Directory.Exists(Path.Combine(candidate, "Moka.Red.Core")))
			{
				return candidate;
			}
		}

		throw new DirectoryNotFoundException("Could not find the src folder above the test output.");
	}

	private sealed record Component(string File, string Css, HashSet<string> Wrappers);

	[GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
	private static partial Regex Comment();

	[GeneratedRegex(@"([^{}]+)\{")]
	private static partial Regex RuleHead();

	[GeneratedRegex(@"\([^()]*\)")]
	private static partial Regex Arguments();

	[GeneratedRegex(@"\.([\w-]+)")]
	private static partial Regex ClassName();

	[GeneratedRegex(@"public\s+RenderFragment(?:<.+?>)?\??\s+(\w+)\s*\{")]
	private static partial Regex Slot();

	[GeneratedRegex(@"<([a-z][\w-]*)\b[^<>]*?(?<![\w-])class=""([^""]*)""[^<>]*>", RegexOptions.Singleline)]
	private static partial Regex Element();

	[GeneratedRegex(@"\G\s*@(\w+)(?:\([^()]*\))?\s*</([a-z][\w-]*)>")]
	private static partial Regex OnlyASlot();

	[GeneratedRegex(@"RootClass\s*=>\s*""([\w-]+)""")]
	private static partial Regex RootClass();

	[GeneratedRegex(@"""([\w-]+)""")]
	private static partial Regex Quoted();

	[GeneratedRegex(@"AddClass\(\s*""([\w-]+)""")]
	private static partial Regex Added();
}
