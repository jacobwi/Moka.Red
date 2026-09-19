using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaThemeToggleTests : BunitContext
{
	[Fact]
	public async Task Click_FlipsTheMode_AndReportsIt()
	{
		bool? reported = null;
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.IsDark, false)
			.Add(x => x.IsDarkChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(reported);
		Assert.Contains("moka-theme-toggle--dark", cut.Find("button").ClassList);
	}

	// The inherited Disabled parameter used to do nothing: the button stayed enabled and switched.
	[Fact]
	public async Task Disabled_DisablesTheButton_AndIgnoresClicks()
	{
		bool? reported = null;
		IRenderedComponent<MokaThemeToggle> cut = Render<MokaThemeToggle>(p => p
			.Add(x => x.IsDark, false)
			.Add(x => x.Disabled, true)
			.Add(x => x.IsDarkChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		IElement button = cut.Find("button");
		await button.ClickAsync(new MouseEventArgs());

		Assert.True(button.HasAttribute("disabled"));
		Assert.Null(reported);
		Assert.DoesNotContain("moka-theme-toggle--dark", cut.Find("button").ClassList);
		Assert.Equal("Switch to dark mode", cut.Find("button").GetAttribute("title"));
	}
}
