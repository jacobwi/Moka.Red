using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaToastMarkupTests : BunitContext
{
	public MokaToastMarkupTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaToastService Toasts => Services.GetRequiredService<IMokaToastService>();

	[Fact]
	public void Open_ShowsTheToastThroughTheService_AndDrawsNothingInPlace()
	{
		IRenderedComponent<MokaToast> cut = Render<MokaToast>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Message, "Saved")
			.Add(x => x.Severity, MokaToastSeverity.Success));

		MokaToastMessage toast = Assert.Single(Toasts.Toasts);
		Assert.Equal("Saved", toast.Message);
		Assert.Equal(MokaToastSeverity.Success, toast.Severity);
		Assert.Empty(cut.Markup.Trim());
	}

	[Fact]
	public void TheHostDrawsIt_WithTheOtherToasts()
	{
		IRenderedComponent<MokaToastHost> host = Render<MokaToastHost>();
		Toasts.ShowInfo("From the service");

		Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "From markup"));

		Assert.Equal(["From the service", "From markup"], host.FindAll(".moka-toast-message").Select(m => m.TextContent));
	}

	[Fact]
	public void NewParameters_UpdateTheToastInPlace()
	{
		IRenderedComponent<MokaToast> cut = Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "Saving"));
		Guid id = Toasts.Toasts[0].Id;

		cut.Render(p => p.Add(x => x.Message, "Saved"));

		MokaToastMessage toast = Assert.Single(Toasts.Toasts);
		Assert.Equal(id, toast.Id);
		Assert.Equal("Saved", toast.Message);
	}

	[Fact]
	public void Closing_TakesTheToastAway_WithoutRaisingOpenChanged()
	{
		List<bool> raised = [];
		IRenderedComponent<MokaToast> cut = Render<MokaToast>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Message, "Saved")
			.Add(x => x.OpenChanged, raised.Add));

		cut.Render(p => p.Add(x => x.Open, false));

		Assert.Empty(Toasts.Toasts);
		Assert.Empty(raised);
	}

	[Fact]
	public void AToastThatGoesOnItsOwn_RaisesOpenChangedAndWhy()
	{
		List<bool> open = [];
		List<MokaToastDismissReason> reasons = [];
		Render<MokaToast>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Message, "Saved")
			.Add(x => x.OpenChanged, open.Add)
			.Add(x => x.OnDismissed, reasons.Add));

		Toasts.Dismiss(Toasts.Toasts[0].Id, MokaToastDismissReason.Closed);

		Assert.Equal([false], open);
		Assert.Equal([MokaToastDismissReason.Closed], reasons);
	}

	[Fact]
	public void AOneWayTrue_DoesntBringItBack_AfterItWentOnItsOwn()
	{
		IRenderedComponent<MokaToast> cut = Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "Saved"));
		Toasts.Dismiss(Toasts.Toasts[0].Id, MokaToastDismissReason.Expired);

		cut.Render(p => p.Add(x => x.Open, true).Add(x => x.Message, "Saved"));

		Assert.Empty(Toasts.Toasts);

		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Single(Toasts.Toasts);
	}

	[Fact]
	public void TheActionRunsOnThePage_AndClosesTheToast()
	{
		int undone = 0;
		IRenderedComponent<MokaToastHost> host = Render<MokaToastHost>();
		Render<MokaToast>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Message, "Deleted")
			.Add(x => x.ActionText, "Undo")
			.Add(x => x.OnAction, () => undone++));

		host.Find(".moka-toast-action").Click();

		Assert.Equal(1, undone);
		Assert.Empty(Toasts.Toasts);
	}

	[Fact]
	public async Task Disposal_TakesTheToastAway()
	{
		Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "Saved"));

		await DisposeComponentsAsync();

		Assert.Empty(Toasts.Toasts);
	}
}
