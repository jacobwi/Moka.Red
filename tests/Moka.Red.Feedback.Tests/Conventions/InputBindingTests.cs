using System.Text.RegularExpressions;

namespace Moka.Red.Feedback.Tests.Conventions;

/// <summary>
///     An input written as <c>value="@x"</c> with <c>@oninput</c> loses keys. Blazor doesn't know what the browser
///     holds, so a render that lands while keys are on their way writes an older value over them: a search box typed
///     into fast came back as "dicker" for "datepicker". <c>@bind:get</c> and <c>@bind:set</c> tell Blazor what the
///     browser holds (components/data-binding.md, "Use @bind:get/@bind:set modifiers and avoid event handlers for
///     two-way data binding"). The listed inputs never hand the typed text back while typing, so they can't race.
/// </summary>
public sealed partial class InputBindingTests
{
	private static readonly string[] ValuePackages = ["Moka.Red.Forms", "Moka.Red.Diagnostics", "Moka.Red.ThemeGen", "Moka.Red.Data"];

	private static readonly string[] ValueNeverChangesWhileTyping =
	[
		"Moka.Red.Data/Table/MokaTable.razor: @RowBoxStart(col)",
		"Moka.Red.Data/Table/MokaTable.razor: @RowBoxStart(col)",
		"Moka.Red.Data/Table/MokaTable.razor: @RowBoxStart(col, second: true)",
		"Moka.Red.Data/Table/MokaTable.razor: @_searchBoxStart",
		"Moka.Red.Forms/InlineEdit/MokaInlineEdit.razor: @_startText"
	];

	// The data package's own inputs follow the same rule as the input bases: the table's boxes and lists, the pager's
	// page size and the table's filter parts.
	[Fact]
	public void NoInput_PairsAValueWithAnInputListener()
	{
		string src = Path.Combine(RepositoryRoot(), "src");
		List<string> found = [];
		foreach (string file in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file).Replace(Path.DirectorySeparatorChar, '/');
			if (relative.Split('/').Any(static part => part is "bin" or "obj"))
			{
				continue;
			}

			string text = File.ReadAllText(file);
			foreach (Match tag in OpeningTag().Matches(text))
			{
				string attributes = Attributes(text, tag.Index + tag.Length);
				Match value = ValueAttribute().Match(attributes);
				if (value.Success && attributes.Contains("@oninput", StringComparison.Ordinal))
				{
					found.Add($"{relative}: {value.Groups[1].Value}");
				}
			}
		}

		Assert.Equal(ValueNeverChangesWhileTyping.Order(StringComparer.Ordinal), found.Order(StringComparer.Ordinal));
	}

	// An input whose parent owns its value puts the parent's value back after a change the parent refuses. With a
	// hand-written value or checked attribute Blazor diffs against its last render, which already held the parent's
	// value, so nothing reaches the browser and the box keeps the refused value. @bind tells Blazor what the browser
	// holds, so that render writes. The same goes for a value a handler clamps or drops, so every input in the
	// packages that edit values (the data package's table and pager included) is checked too, whatever it inherits. Radios aren't listed: a radio's change carries
	// its value, not whether it's checked.
	[Fact]
	public void NoValueInput_PairsAValueWithAChangeListener()
	{
		string src = Path.Combine(RepositoryRoot(), "src");
		List<string> found = [];
		foreach (string file in Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(src, file).Replace(Path.DirectorySeparatorChar, '/');
			string text = File.ReadAllText(file);
			bool editsValues = InputBase().IsMatch(text) || ValuePackages.Contains(relative.Split('/')[0]);
			if (relative.Split('/').Any(static part => part is "bin" or "obj") || !editsValues)
			{
				continue;
			}

			foreach (Match tag in ValueElementTag().Matches(text))
			{
				string attributes = Attributes(text, tag.Index + tag.Length);
				Match value = ValueOrChecked().Match(attributes);
				if (value.Success && attributes.Contains("@onchange", StringComparison.Ordinal)
				    && !attributes.Contains("type=\"radio\"", StringComparison.Ordinal))
				{
					found.Add($"{relative}: {value.Value}");
				}
			}
		}

		Assert.Empty(found);
	}

	// Up to the tag's closing '>', skipping quoted text, where a lambda's '=>' would otherwise end it.
	private static string Attributes(string text, int start)
	{
		char? quote = null;
		for (int i = start; i < text.Length; i++)
		{
			char c = text[i];
			if (quote is not null)
			{
				if (c == quote)
				{
					quote = null;
				}
			}
			else if (c is '"' or '\'')
			{
				quote = c;
			}
			else if (c == '>')
			{
				return text[start..i];
			}
		}

		return text[start..];
	}

	[GeneratedRegex(@"<(?:input|textarea)\b")]
	private static partial Regex OpeningTag();

	[GeneratedRegex(@"(?<![:\w-])value=""([^""]*)""")]
	private static partial Regex ValueAttribute();

	[GeneratedRegex(@"<(?:input|select|textarea)\b")]
	private static partial Regex ValueElementTag();

	[GeneratedRegex(@"(?<![:\w-])(?:value|checked)=""@[^""]*""")]
	private static partial Regex ValueOrChecked();

	[GeneratedRegex(@"@inherits\s+Moka(?:Visual)?(?:Input|TextInput|Select|Toggle|SegmentedInput)Base\b")]
	private static partial Regex InputBase();

	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				return folder.FullName;
			}
		}

		throw new InvalidOperationException("The repository root was not found.");
	}
}
