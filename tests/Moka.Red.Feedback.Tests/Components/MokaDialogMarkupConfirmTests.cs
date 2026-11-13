using System.Diagnostics.CodeAnalysis;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// A confirm that needed a lead line and a few facts in mono had to be a whole component dialog, since ConfirmAsync took
// only text. The markup overload is the same confirm with your own body.
public class MokaDialogMarkupConfirmTests : BunitContext
{
	private static readonly RenderFragment Facts = builder =>
	{
		builder.OpenElement(0, "p");
		builder.AddAttribute(1, "class", "lead");
		builder.AddContent(2, "A plugin wants to save a file.");
		builder.CloseElement();
		builder.OpenElement(3, "code");
		builder.AddContent(4, "report.pdf, 2.4 MB");
		builder.CloseElement();
	};

	public MokaDialogMarkupConfirmTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaDialogService Dialogs => Services.GetRequiredService<IMokaDialogService>();

	[Fact]
	public void TheBody_IsTheMarkup_AndTheFooterIsTheConfirms()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		_ = Dialogs.ConfirmAsync(Facts, "Save file?", o => o.ConfirmText = "Save");
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		IElement body = cut.Find(".moka-dialog-body");
		Assert.Equal("A plugin wants to save a file.", body.QuerySelector("p.lead")?.TextContent);
		Assert.Equal("report.pdf, 2.4 MB", body.QuerySelector("code")?.TextContent);
		Assert.Empty(body.QuerySelectorAll("input"));
		Assert.Equal(["Cancel", "Save"], cut.FindAll(".moka-dialog-actions button").Select(b => b.TextContent.Trim()));
		Assert.Equal("Save file?", cut.Find(".moka-dialog-title").TextContent.Trim());
	}

	[Fact]
	public async Task Confirming_ReturnsTrue()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync(Facts);
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "OK").ClickAsync(new MouseEventArgs());

		Assert.True(await answer);
	}

	[Fact]
	public async Task Cancelling_ReturnsFalse()
	{
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync(Facts, "Save file?");
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "Cancel").ClickAsync(new MouseEventArgs());

		Assert.False(await answer);
	}

	[Fact]
	public void WithoutATitle_ItSaysConfirm()
	{
		_ = Dialogs.ConfirmAsync(Facts);

		Assert.Equal("Confirm", Assert.Single(Dialogs.OpenDialogs).Title);
		Assert.Equal(MokaDialogType.Confirm, Dialogs.OpenDialogs[0].Type);
	}

	// The same work, busy state and failure as the text confirm.
	[Fact]
	public async Task OnConfirmAsync_RunsWithTheDialogOpen_AndAFailureKeepsItOpen()
	{
		int tries = 0;
		IRenderedComponent<MokaDialogHost> cut = Render<MokaDialogHost>();
		Task<bool> answer = Dialogs.ConfirmAsync(Facts, "Save file?", o =>
		{
			o.ConfirmText = "Save";
			o.OnConfirmAsync = async _ =>
			{
				await Task.Yield();
				if (++tries == 1)
				{
					throw new InvalidOperationException("The disk is full.");
				}
			};
		});
		cut.WaitForAssertion(() => cut.Find("[role=dialog]"));

		await Button(cut, "Save").ClickAsync(new MouseEventArgs());
		cut.WaitForAssertion(() => Assert.Equal("The disk is full.", cut.Find(".moka-dialog-failure").TextContent.Trim()));
		Assert.False(answer.IsCompleted);
		Assert.NotNull(cut.Find(".moka-dialog-body code"));

		await Button(cut, "Save").ClickAsync(new MouseEventArgs());

		Assert.True(await answer);
		Assert.Equal(2, tries);
	}

	[Fact]
	public async Task NoContent_Throws() =>
		await Assert.ThrowsAsync<ArgumentNullException>(() => Dialogs.ConfirmAsync((RenderFragment)null!));

	// An implementation written before the overload still compiles, and says plainly that it can't.
	[Fact]
	public async Task AnOlderImplementation_SaysItCant()
	{
		IMokaDialogService older = new OlderDialogs();

		NotSupportedException failure = await Assert.ThrowsAsync<NotSupportedException>(() => older.ConfirmAsync(Facts));
		Assert.Contains(nameof(OlderDialogs), failure.Message, StringComparison.Ordinal);
	}

	private static IElement Button(IRenderedComponent<MokaDialogHost> cut, string text) =>
		cut.FindAll("[role=dialog] .moka-dialog-actions button").Single(b => b.TextContent.Trim() == text);

	private sealed class OlderDialogs : IMokaDialogService
	{
#pragma warning disable CS0067 // An empty fake: nothing is ever raised.
		public event Action<MokaDialogRequest>? OnDialogRequested;
		public event Action? OnDialogClosed;
#pragma warning restore CS0067

		public IReadOnlyList<MokaDialogRequest> OpenDialogs => [];

		public Task<bool> ConfirmAsync(string message, string? title = null, Action<MokaDialogOptions>? configure = null) =>
			Task.FromResult(false);

		public Task<string?> PromptAsync(string message, string? title = null, string? defaultValue = null) =>
			Task.FromResult<string?>(null);

		public Task<string?> PromptAsync(string message, string? title, string? defaultValue, Action<MokaDialogOptions>? configure) =>
			Task.FromResult<string?>(null);

		public Task ShowAsync(string title, RenderFragment content, Action<MokaDialogOptions>? configure = null) => Task.CompletedTask;

		public Task<object?> ShowComponentAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(string title, Action<Dictionary<string, object>>? parameters = null,
			Action<MokaDialogOptions>? configure = null) where TComponent : IComponent => Task.FromResult<object?>(null);

		public void Close(bool result = false)
		{
		}

		public void CloseWithResult(object? result)
		{
		}

		public void Close(MokaDialogRequest request, bool result = false)
		{
		}

		public void CloseWithResult(MokaDialogRequest request, object? result)
		{
		}

		public void CloseAll()
		{
		}
	}
}
