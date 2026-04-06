using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Chat;

/// <summary>
///     Chat message list with avatars, timestamps, typing indicator, and input.
///     Supports custom message templates and auto-scroll on new messages.
/// </summary>
public partial class MokaChat : MokaVisualComponentBase
{
	private string _inputText = string.Empty;
	private int _lastMessageCount;
	private ElementReference _messageListRef;

	/// <summary>The list of chat messages to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaChatMessage> Messages { get; set; } = [];

	/// <summary>Fires when the user sends a message.</summary>
	[Parameter]
	public EventCallback<string> OnSend { get; set; }

	/// <summary>Whether to show the input area. Default true.</summary>
	[Parameter]
	public bool ShowInput { get; set; } = true;

	/// <summary>Placeholder text for the input field. Default "Type a message...".</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Type a message...";

	/// <summary>Whether to show timestamps on messages. Default true.</summary>
	[Parameter]
	public bool ShowTimestamps { get; set; } = true;

	/// <summary>Whether to show avatars on messages. Default true.</summary>
	[Parameter]
	public bool ShowAvatars { get; set; } = true;

	/// <summary>Whether to show sent/delivered/read indicators. Default true.</summary>
	[Parameter]
	public bool ShowStatus { get; set; } = true;

	/// <summary>Whether someone is currently typing. Default false.</summary>
	[Parameter]
	public bool IsTyping { get; set; }

	/// <summary>Display name of the user who is typing.</summary>
	[Parameter]
	public string? TypingUser { get; set; }

	/// <summary>Height of the chat container. Default "400px".</summary>
	[Parameter]
	public string Height { get; set; } = "400px";

	/// <summary>Whether to auto-scroll to the bottom on new messages. Default true.</summary>
	[Parameter]
	public bool AutoScroll { get; set; } = true;

	/// <summary>Custom template for rendering individual messages.</summary>
	[Parameter]
	public RenderFragment<MokaChatMessage>? MessageTemplate { get; set; }

	/// <summary>Optional header content above the messages area (e.g., chat name, status).</summary>
	[Parameter]
	public RenderFragment? HeaderContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-chat";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-chat--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Override ShouldRender to always return true for message updates.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (AutoScroll && Messages.Count != _lastMessageCount)
		{
			_lastMessageCount = Messages.Count;
			await ScrollToBottomAsync();
		}

		if (firstRender && AutoScroll && Messages.Count > 0)
		{
			await ScrollToBottomAsync();
		}
	}

	private async Task ScrollToBottomAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
			await module.InvokeVoidAsync("scrollToBottom", _messageListRef);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected
		}
	}

	private async Task HandleSend()
	{
		string text = _inputText.Trim();
		if (string.IsNullOrEmpty(text) || !OnSend.HasDelegate)
		{
			return;
		}

		_inputText = string.Empty;
		await OnSend.InvokeAsync(text);
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.ShiftKey)
		{
			await HandleSend();
		}
	}

	private static string GetStatusIcon(MokaChatMessageStatus status) => status switch
	{
		MokaChatMessageStatus.Sending => "\u2022",
		MokaChatMessageStatus.Sent => "\u2713",
		MokaChatMessageStatus.Delivered => "\u2713\u2713",
		MokaChatMessageStatus.Read => "\u2713\u2713",
		MokaChatMessageStatus.Error => "\u2717",
		_ => string.Empty
	};

	private static string FormatTimestamp(DateTime timestamp)
	{
		DateTime local = timestamp.ToLocalTime();
		return local.ToString("h:mm tt", CultureInfo.InvariantCulture);
	}
}
