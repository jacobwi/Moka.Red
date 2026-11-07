using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.ErrorBoundary;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaErrorBoundaryTests : BunitContext
{
	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	public MokaErrorBoundaryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static RenderFragment Child(Func<bool> fail) => builder =>
	{
		builder.OpenComponent<FailingChild>(0);
		builder.AddComponentParameter(1, nameof(FailingChild.Fail), fail);
		builder.CloseComponent();
	};

	private IRenderedComponent<MokaErrorBoundary> Boundary(
		Func<bool> fail,
		Action<ComponentParameterCollectionBuilder<MokaErrorBoundary>>? more = null) =>
		Render<MokaErrorBoundary>(p =>
		{
			p.Add(x => x.ChildContent, Child(fail));
			more?.Invoke(p);
		});

	[Fact]
	public void WorkingContent_GetsNoWrapper()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => false);

		Assert.Equal("Working", cut.Markup.Trim());
	}

	[Fact]
	public void AFailure_IsCaught_AndCarriesTheStableClass()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		Assert.Contains("moka-error-boundary", cut.Find(".moka-error-boundary--failed").ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("Working", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void TheFullLook_NamesWhatStopped_DefaultingToThePage()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		Assert.Equal("This page hit a problem", cut.Find("h2").TextContent);
		Assert.Equal("alert", cut.Find(".moka-error-boundary--failed").GetAttribute("role"));
		Assert.Single(cut.FindAll(".moka-error-boundary--full"));
	}

	[Fact]
	public void What_ChangesTheSentence()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p.Add(x => x.What, "This card"));

		Assert.Equal("This card hit a problem", cut.Find("h2").TextContent);
	}

	[Fact]
	public void TheFullLook_OffersBothWaysOut()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		Assert.Equal(["Try again", "Reload the page"], cut.FindAll("button").Select(b => b.TextContent.Trim()));
	}

	[Fact]
	public void TheFullLook_KeepsTheMessageInAFold()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		IElement fold = cut.Find("details");
		Assert.False(fold.HasAttribute("open"));
		Assert.Equal("The cable fell out", fold.QuerySelector("pre")!.TextContent);
	}

	[Fact]
	public void ShowMessageOff_LeavesTheFoldOut()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p.Add(x => x.ShowMessage, false));

		Assert.Empty(cut.FindAll("details"));
		Assert.DoesNotContain("The cable fell out", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void Links_SitBesideTheButtons()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p
			.Add(x => x.Links, b => b.AddMarkupContent(0, "<a href=\"logs\">See the logs</a>")));

		Assert.Equal("See the logs", cut.Find(".moka-error-boundary__actions a").TextContent);
	}

	[Fact]
	public void TryAgain_ShowsTheContentOnceItWorks()
	{
		bool fail = true;
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => fail);
		fail = false;

		cut.FindAll("button")[0].Click();

		Assert.Equal("Working", cut.Markup.Trim());
	}

	[Fact]
	public void TryAgain_FailsAgainWithoutRethrowing_WhileItStillFails()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		cut.FindAll("button")[0].Click();

		Assert.Single(cut.FindAll(".moka-error-boundary--failed"));
	}

	[Fact]
	public void Reload_LoadsThePageAgain()
	{
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		cut.FindAll("button")[1].Click();

		Assert.True(navigation.History.First().Options.ForceLoad);
	}

	[Fact]
	public void TheCompactLook_IsOneLine()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p
			.Add(x => x.Compact, true)
			.Add(x => x.What, "This chart"));

		Assert.Equal("This chart stopped working.", cut.Find(".moka-error-boundary__line").TextContent);
		Assert.Equal("Try again", cut.Find("button").TextContent);
		Assert.Single(cut.FindAll("button"));
		Assert.Single(cut.FindAll(".moka-error-boundary--failed.moka-error-boundary--compact"));
		Assert.Empty(cut.FindAll("details"));
		Assert.Empty(cut.FindAll("h2"));
	}

	[Fact]
	public void TheCompactLook_TriesAgainToo()
	{
		bool fail = true;
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => fail, p => p.Add(x => x.Compact, true));
		fail = false;

		cut.Find("button").Click();

		Assert.Equal("Working", cut.Markup.Trim());
	}

	[Fact]
	public void ErrorContent_ReplacesTheLook_AndKeepsTheStableClass()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p
			.Add(x => x.ErrorContent, problem => b => b.AddContent(0, $"Sorry: {problem.Message}")));

		IElement failed = cut.Find(".moka-error-boundary--failed");
		Assert.Equal("Sorry: The cable fell out", failed.TextContent.Trim());
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public void TheFailureMarkup_TakesClassStyleIdAndAttributes()
	{
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true, p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem")
			.Add(x => x.Id, "boundary")
			.AddUnmatched("data-area", "main"));

		IElement failed = cut.Find(".moka-error-boundary--failed");
		Assert.EndsWith("mine", failed.ClassName, StringComparison.Ordinal);
		Assert.Equal("max-width: 30rem", failed.GetAttribute("style"));
		Assert.Equal("boundary", failed.Id);
		Assert.Equal("main", failed.GetAttribute("data-area"));
	}

	[Fact]
	public void RecoverOnNavigation_TriesAgainWhenTheLocationChanges()
	{
		bool fail = true;
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => fail, p => p.Add(x => x.RecoverOnNavigation, true));
		fail = false;

		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/elsewhere");

		cut.WaitForAssertion(() => Assert.Equal("Working", cut.Markup.Trim()), Plenty);
	}

	// A router with an OnNavigateAsync hands the new page over after the location changed, so the old page fails once more.
	[Fact]
	public void RecoverOnNavigation_TriesTheContentThatArrivesAfterTheLocationChanged()
	{
		bool fail = true;
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => fail, p => p.Add(x => x.RecoverOnNavigation, true));
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();
		navigation.NavigateTo("/elsewhere");
		cut.WaitForAssertion(() => Assert.NotEmpty(navigation.History), Plenty);
		Assert.Single(cut.FindAll(".moka-error-boundary--failed"));
		fail = false;

		cut.Render(p => p.Add(x => x.RecoverOnNavigation, true));

		Assert.Equal("Working", cut.Markup.Trim());
	}

	[Fact]
	public void WithoutRecoverOnNavigation_AFailureStays()
	{
		bool fail = true;
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => fail);
		fail = false;
		BunitNavigationManager navigation = Services.GetRequiredService<BunitNavigationManager>();

		navigation.NavigateTo("/elsewhere");
		cut.WaitForAssertion(() => Assert.NotEmpty(navigation.History), Plenty);

		Assert.Single(cut.FindAll(".moka-error-boundary--failed"));
	}

	[Fact]
	public void TheFailure_ReachesTheLogger()
	{
		RecordingLogger logger = new();
		Services.AddSingleton<IErrorBoundaryLogger>(logger);

		Boundary(() => true);

		Assert.Equal("The cable fell out", Assert.Single(logger.Logged).Message);
	}

	// The base class rethrows what OnErrorAsync throws by rendering it, which would take the page down after all.
	[Fact]
	public void ALoggerThatThrows_ChangesNothing()
	{
		Services.AddSingleton<IErrorBoundaryLogger>(new RecordingLogger { FailWith = new InvalidOperationException("No disk") });

		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		Assert.Equal("The cable fell out", cut.Find("details pre").TextContent);
	}

	[Fact]
	public void ALoggerThatFailsLater_ChangesNothing()
	{
		TaskCompletionSource logged = new();
		Services.AddSingleton<IErrorBoundaryLogger>(new RecordingLogger { Waits = logged.Task });
		IRenderedComponent<MokaErrorBoundary> cut = Boundary(() => true);

		logged.SetException(new InvalidOperationException("No disk"));

		cut.WaitForAssertion(() => Assert.Equal("The cable fell out", cut.Find("details pre").TextContent), Plenty);
	}

	public sealed class FailingChild : ComponentBase
	{
		[Parameter]
		public Func<bool> Fail { get; set; } = () => false;

		protected override void OnParametersSet()
		{
			if (Fail())
			{
				throw new InvalidOperationException("The cable fell out");
			}
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.AddContent(0, "Working");
		}
	}

	private sealed class RecordingLogger : IErrorBoundaryLogger
	{
		public List<Exception> Logged { get; } = [];

		public Exception? FailWith { get; init; }

		public Task? Waits { get; init; }

		public async ValueTask LogErrorAsync(Exception exception)
		{
			Logged.Add(exception);
			if (Waits is not null)
			{
				await Waits;
			}

			if (FailWith is not null)
			{
				throw FailWith;
			}
		}
	}
}
