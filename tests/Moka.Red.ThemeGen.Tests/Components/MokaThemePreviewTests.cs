using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.NotificationBell;
using Moka.Red.Feedback.Toast;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Forms.Switch;
using Moka.Red.Forms.TextField;
using Moka.Red.Layout.Card;
using Moka.Red.Layout.DockLayout;
using Moka.Red.Layout.PageHeader;
using Moka.Red.Layout.StatusBar;
using Moka.Red.Navigation.Menu;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.Tag;
using Moka.Red.ThemeGen.Preview;

namespace Moka.Red.ThemeGen.Tests.Components;

public sealed class MokaThemePreviewTests : BunitContext
{
	public MokaThemePreviewTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		SetRendererInfo(new RendererInfo("Server", true));
	}

	[Fact]
	public void ItDraws_TheLibrarysOwnComponents_ThroughARealProvider()
	{
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, MokaTheme.GateLight));

		Assert.Equal(MokaTheme.GateLight, cut.FindComponent<MokaThemeProvider>().Instance.Theme);
		Assert.NotNull(cut.FindComponent<MokaPageHeader>());
		Assert.NotNull(cut.FindComponent<MokaTabs>());
		Assert.True(cut.FindComponents<MokaButton>().Count >= 8);
		Assert.Equal(2, cut.FindComponents<MokaTextField>().Count);
		Assert.NotNull(cut.FindComponent<MokaSelect<string>>());
		Assert.NotNull(cut.FindComponent<MokaSwitch>());
		Assert.NotNull(cut.FindComponent<MokaCheckbox>());
		Assert.Equal(4, cut.FindComponents<MokaAlert>().Count);
		Assert.Single(cut.FindAll("table"));
		Assert.NotNull(cut.FindComponent<MokaBarChart>());
		Assert.NotEmpty(cut.FindComponents<MokaTag>());
		Assert.Contains("moka-root", cut.Find(".moka-root").ClassName, StringComparison.Ordinal);
	}

	// The parts that show the style choices nothing else in the sample used: depth, the current page, selected items,
	// settings headers, the status bar look and the tooltip choices.
	[Fact]
	public void EveryStyleChoice_HasSomethingToShowItOn()
	{
		MokaThemePreviewText text = new();
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, MokaTheme.Red));

		Assert.Contains(cut.FindComponents<MokaCard>(), card => card.Instance.Elevation >= 3);
		Assert.Equal(text.Projects[0], cut.Find(".moka-menu [aria-current]").TextContent.Trim());
		Assert.Single(cut.FindAll("tr.moka-table-row--selected"));
		Assert.Equal(text.SettingsNow, cut.FindComponent<MokaSettingsForm>().Instance.Now);
		Assert.False(cut.FindComponent<MokaStatusBar>().Instance.Fixed);
		Assert.Contains(cut.FindComponents<MokaStatusBarItem>(), item => item.Instance.Tooltip == text.SyncedTooltip);
	}

	// The notification list is drawn in place and the dock layout never narrows, so their looks show without opening
	// anything.
	[Fact]
	public void TheNotificationAndDockLooks_ShowWithoutOpeningAnything()
	{
		MokaTheme theme = MokaTheme.Red.WithNotifications(MokaNotificationLook.Grouped).WithDocks(MokaDockLook.Ide);
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, theme));

		IElement list = cut.Find(".moka-notification-bell--inline");
		Assert.Contains("moka-notification-bell--grouped", list.ClassName, StringComparison.Ordinal);
		Assert.Empty(list.QuerySelectorAll(".moka-notification-bell__trigger"));
		Assert.Contains(new MokaThemePreviewText().Bell.Earlier, list.QuerySelectorAll(".moka-notification-bell__group").Select(group => group.TextContent.Trim()));
		Assert.Contains("moka-dock-layout--ide", cut.Find(".moka-dock-layout").ClassName, StringComparison.Ordinal);
		Assert.Null(cut.FindComponent<MokaDockLayout>().Instance.NarrowBelow);
	}

	[Fact]
	public void MarkAllRead_LeavesNothingUnread()
	{
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, MokaTheme.Red));
		Assert.Equal(2, cut.FindAll(".moka-notification-bell__item--unread").Count);

		cut.FindAll(".moka-notification-bell__action").First(button => button.TextContent.Trim() == new MokaNotificationBellText().MarkAllRead).Click(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-notification-bell__item--unread"));
	}

	[Fact]
	public void PickingAnotherProject_MovesTheCurrentPage()
	{
		MokaThemePreviewText text = new();
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>();

		cut.FindAll(".moka-menu button").First(button => button.TextContent.Trim() == text.Projects[2]).Click(new MouseEventArgs());

		Assert.Equal(text.Projects[2], cut.Find(".moka-menu [aria-current]").TextContent.Trim());
	}

	// Only an app's outermost provider writes the button looks, so the sample's own buttons carry the previewed ones.
	[Fact]
	public void TheSamplesButtons_TakeThePreviewedButtonLooks()
	{
		MokaTheme theme = MokaTheme.Red.WithButtons(MokaButtonLook.Pill).WithDisabledButtons(MokaDisabledButtonLook.Outline);
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, theme));

		IReadOnlyList<IRenderedComponent<MokaButton>> buttons = cut.FindComponents<MokaButton>();
		Assert.All(buttons, button => Assert.Equal(MokaButtonLook.Pill, button.Instance.Look));
		Assert.All(buttons, button => Assert.Equal(MokaDisabledButtonLook.Outline, button.Instance.DisabledLook));
	}

	// The toggles show the previewed theme's pressed choices, which the outermost provider would otherwise decide.
	[Fact]
	public void TheSamplesToggles_ArePressed_InThePreviewedPressedLooks()
	{
		MokaTheme theme = MokaTheme.Red.WithPressedButtons(MokaPressedButtonLook.Ring).WithPressedButtonFill(MokaPressedButtonFill.Solid);
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p.Add(x => x.PreviewTheme, theme));

		List<IRenderedComponent<MokaButton>> toggles = [.. cut.FindComponents<MokaButton>().Where(button => button.Instance.Pressed is not null)];
		Assert.Equal(2, toggles.Count);
		Assert.All(toggles, toggle => Assert.Equal("true", toggle.Find("button").GetAttribute("aria-pressed")));
		Assert.All(toggles, toggle => Assert.Equal(MokaPressedButtonLook.Ring, toggle.Instance.PressedLook));
		Assert.All(toggles, toggle => Assert.Equal(MokaPressedButtonFill.Solid, toggle.Instance.PressedFill));
	}

	[Fact]
	public void TheDialog_OpensInsideThePreviewsStage()
	{
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>();

		cut.FindAll("button").First(button => button.TextContent.Trim() == new MokaThemePreviewText().OpenDialog).Click(new MouseEventArgs());

		Assert.True(cut.FindComponent<MokaDialog>().Instance.Open);
		Assert.NotNull(cut.Find(".moka-theme-preview__stage [role='dialog']"));
		Assert.False(cut.FindComponent<MokaDialog>().Instance.PreventScroll);
	}

	[Fact]
	public void WithoutTheToastService_ThereIsNoToastButton()
	{
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>();

		Assert.Empty(cut.FindComponents<MokaToastHost>());
		Assert.DoesNotContain(new MokaThemePreviewText().ShowToast, cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void AToast_ShowsInsideThePreviewsStage()
	{
		using BunitContext context = new();
		context.JSInterop.Mode = JSRuntimeMode.Loose;
		context.Services.AddMokaFeedback();
		context.SetRendererInfo(new RendererInfo("Server", true));
		IRenderedComponent<MokaThemePreview> cut = context.Render<MokaThemePreview>();

		cut.FindAll("button").First(button => button.TextContent.Trim() == new MokaThemePreviewText().ShowToast).Click(new MouseEventArgs());

		Assert.Contains(new MokaThemePreviewText().ToastMessage, cut.Find(".moka-theme-preview__stage").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Words_ComeFromTheText()
	{
		IRenderedComponent<MokaThemePreview> cut = Render<MokaThemePreview>(p => p
			.Add(x => x.Text, new MokaThemePreviewText
			{
				Title = "Lancement", People = ["Ana"], Bell = new MokaNotificationBellText { Title = "Avis" }, WorkspaceFiles = ["notes.txt"]
			}));

		Assert.Contains("Lancement", cut.Find(".moka-page-header").TextContent, StringComparison.Ordinal);
		Assert.Contains("Ana", cut.Markup, StringComparison.Ordinal);
		Assert.Equal("Avis", cut.Find(".moka-notification-bell__title").TextContent.Trim());
		Assert.Contains("notes.txt", cut.Find(".moka-theme-preview__dock").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ThePreviewsStyles_ReadOnlyTokens()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaThemePreview.razor.css"));

		Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", css);
		Assert.DoesNotMatch(@"\d+px", css);
	}
}
