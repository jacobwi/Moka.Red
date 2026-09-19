using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.Tests.Services;

public class MokaNotificationServiceTests
{
	// Push added a second entry with the same id, so MarkAsRead changed only the first and Remove
	// took both.
	[Fact]
	public void PushingAnIdAgain_ReplacesTheNotification_InItsPlace()
	{
		var service = new MokaNotificationService();
		var id = Guid.NewGuid();
		service.Push(new MokaNotification { Id = id, Title = "Build", Message = "Running" });
		service.Push(new MokaNotification { Title = "Other", Message = "Unrelated" });

		service.Push(new MokaNotification { Id = id, Title = "Build", Message = "Passed" });

		Assert.Equal(2, service.Notifications.Count);
		Assert.Equal(id, service.Notifications[0].Id);
		Assert.Equal("Passed", service.Notifications[0].Message);
	}

	[Fact]
	public void MarkAsRead_AndRemove_ActOnTheOneEntry()
	{
		var service = new MokaNotificationService();
		var id = Guid.NewGuid();
		service.Push(new MokaNotification { Id = id, Title = "Build", Message = "Running" });
		service.Push(new MokaNotification { Id = id, Title = "Build", Message = "Passed" });

		service.MarkAsRead(id);
		Assert.Equal(0, service.UnreadCount);

		service.Remove(id);
		Assert.Empty(service.Notifications);
	}
}
