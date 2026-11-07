using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.ErrorBoundary;

/// <summary>
///     Catches what its content throws and says so in the app's look, with a way to try again, so one failing part
///     doesn't end the Blazor Server circuit. Whatever it draws while failed carries the class
///     <c>moka-error-boundary--failed</c>, so a test can fail on it.
/// </summary>
public sealed partial class MokaErrorBoundary : ErrorBoundaryBase, IDisposable
{
	private const string RootClass = "moka-error-boundary";

	private bool _retryOnNextParameters;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>What stopped, for the sentence: "This page", "This card", "The menu".</summary>
	[Parameter]
	public string What { get; set; } = "This page";

	/// <summary>One line, for a part of a page rather than all of it.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>Beside the buttons of the full look, for links such as where the logs are.</summary>
	[Parameter]
	public RenderFragment? Links { get; set; }

	/// <summary>
	///     Tries the content again when the location changes, so a failed page doesn't keep the next one from showing.
	///     Put this on the boundary around a layout's body.
	/// </summary>
	[Parameter]
	public bool RecoverOnNavigation { get; set; }

	/// <summary>The line under the title of the full look.</summary>
	[Parameter]
	public string? Detail { get; set; } = "The rest of the app is fine.";

	/// <summary>Whether the full look has its fold with the exception's message. Turn it off if messages can hold anything private.</summary>
	[Parameter]
	public bool ShowMessage { get; set; } = true;

	/// <summary>The label of the fold with the exception's message.</summary>
	[Parameter]
	public string MessageLabel { get; set; } = "What went wrong";

	/// <summary>The text of the button that tries the content again.</summary>
	[Parameter]
	public string TryAgainText { get; set; } = "Try again";

	/// <summary>The text of the button that reloads the page.</summary>
	[Parameter]
	public string ReloadText { get; set; } = "Reload the page";

	/// <summary>Additional CSS classes on what's drawn while failed. Working content gets no wrapper.</summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>Additional inline styles on what's drawn while failed.</summary>
	[Parameter]
	public string? Style { get; set; }

	/// <summary>The id of what's drawn while failed.</summary>
	[Parameter]
	public string? Id { get; set; }

	/// <summary>Additional attributes on what's drawn while failed.</summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	private string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-error-boundary--failed")
		.AddClass(Compact ? "moka-error-boundary--compact" : "moka-error-boundary--full", ErrorContent is null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	public void Dispose() => Navigation.LocationChanged -= HandleLocationChanged;

	/// <inheritdoc />
	protected override void OnInitialized() => Navigation.LocationChanged += HandleLocationChanged;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (_retryOnNextParameters)
		{
			_retryOnNextParameters = false;
			Recover();
		}
	}

	/// <inheritdoc />
	protected override async Task OnErrorAsync(Exception exception)
	{
		// Not every host registers one. It's what puts the exception in the server's log.
		IErrorBoundaryLogger? logger = Services.GetService<IErrorBoundaryLogger>();
		if (logger is null)
		{
			return;
		}

		try
		{
			await logger.LogErrorAsync(exception);
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			// The base class rethrows what this method throws by rendering it, which would take down the page this
			// boundary just saved. Logging is the one part nobody needs to see fail.
		}
	}

	// A router with an OnNavigateAsync still holds the old page when this runs. That page fails again, and the new one
	// arrives as parameters a moment later, so those get a try of their own.
	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		if (!RecoverOnNavigation)
		{
			return;
		}

		_ = InvokeAsync(() =>
		{
			_retryOnNextParameters = true;
			Recover();
		});
	}

	private void Reload() => Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
}
