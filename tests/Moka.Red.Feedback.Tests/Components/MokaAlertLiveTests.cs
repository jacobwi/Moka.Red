using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Alert;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Components;

// Up to 0.4.2 every alert was role="alert", read out over whatever the screen reader was saying, even a note that
// sits on the page from the start.
public class MokaAlertLiveTests : BunitContext
{
	[Fact]
	public void ByDefault_TheAlertInterrupts()
	{
		IElement alert = Render<MokaAlert>(p => p.AddChildContent("Saved")).Find(".moka-alert");

		Assert.Equal("alert", alert.GetAttribute("role"));
	}

	[Theory]
	[InlineData(MokaAlertLive.Alert, "alert")]
	[InlineData(MokaAlertLive.Status, "status")]
	public void Live_PicksTheRole(MokaAlertLive live, string role)
	{
		IElement alert = Render<MokaAlert>(p => p.Add(x => x.Live, live)).Find(".moka-alert");

		Assert.Equal(role, alert.GetAttribute("role"));
	}

	[Fact]
	public void None_WritesNoRole()
	{
		IElement alert = Render<MokaAlert>(p => p.Add(x => x.Live, MokaAlertLive.None)).Find(".moka-alert");

		Assert.False(alert.HasAttribute("role"));
		Assert.False(alert.HasAttribute("aria-live"));
	}

	[Theory]
	[InlineData(MokaAlertLive.Alert, "alert")]
	[InlineData(MokaAlertLive.Status, "status")]
	[InlineData(MokaAlertLive.None, null)]
	public void Accent_KeepsTheRoleLiveAsks(MokaAlertLive live, string? role)
	{
		IElement alert = Render<MokaAlert>(p => p
			.Add(x => x.Accent, true)
			.Add(x => x.Live, live)).Find(".moka-alert");

		Assert.Equal(role, alert.GetAttribute("role"));
		Assert.Equal("moka-alert moka-alert--accent", alert.ClassName);
	}

	[Theory]
	[InlineData(MokaAlertLive.Alert)]
	[InlineData(MokaAlertLive.Status)]
	[InlineData(MokaAlertLive.None)]
	public void Live_DoesNotChangeTheLook(MokaAlertLive live)
	{
		IElement alert = Render<MokaAlert>(p => p
			.Add(x => x.Severity, MokaToastSeverity.Warning)
			.Add(x => x.Live, live)).Find(".moka-alert");

		Assert.Equal("moka-alert moka-alert--warning", alert.ClassName);
	}

	[Fact]
	public void AnAppsOwnRole_StillWins()
	{
		IElement alert = Render<MokaAlert>(p => p
			.Add(x => x.Live, MokaAlertLive.None)
			.AddUnmatched("role", "note")).Find(".moka-alert");

		Assert.Equal("note", alert.GetAttribute("role"));
	}
}
