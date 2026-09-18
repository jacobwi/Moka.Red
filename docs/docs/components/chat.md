---
title: Chat
description: Message thread with avatars, timestamps, delivery status, a typing indicator and a send box.
order: 88
---

# Chat

`MokaChat` renders a message thread and an input box. It shows each message's author, avatar, time and delivery status, scrolls to the newest message, and raises `OnSend` with the text the user sends. The component does not keep messages itself: add the sent text to `Messages` in your handler.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Messages` | `IReadOnlyList<MokaChatMessage>` | empty | The thread, oldest first |
| `OnSend` | `EventCallback<string>` | -- | Fires with the trimmed text when the user sends |
| `ShowInput` | `bool` | `true` | Shows the input box and send button |
| `Placeholder` | `string` | `"Type a message..."` | Input placeholder |
| `ShowTimestamps` | `bool` | `true` | Shows the time on each message |
| `ShowAvatars` | `bool` | `true` | Shows author avatars |
| `ShowStatus` | `bool` | `true` | Shows sent, delivered and read ticks on your own messages |
| `IsTyping` | `bool` | `false` | Shows the typing indicator |
| `TypingUser` | `string?` | -- | Name shown with the typing indicator |
| `Height` | `string` | `"400px"` | Height of the component |
| `AutoScroll` | `bool` | `true` | Scrolls to the bottom when messages are added |
| `MessageTemplate` | `RenderFragment<MokaChatMessage>?` | -- | Custom rendering for each message |
| `HeaderContent` | `RenderFragment?` | -- | Content above the thread, such as the chat name |

### MokaChatMessage

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Required. Unique per message |
| `Text` | `string` | Required. The message text |
| `Author` | `string` | Required. Display name |
| `AvatarUri` | `Uri?` | Avatar image |
| `AvatarInitials` | `string?` | Shown when there is no image |
| `Timestamp` | `DateTime` | Defaults to `DateTime.UtcNow` |
| `IsOwn` | `bool` | Sent by the current user: right-aligned, with status ticks |
| `IsSystem` | `bool` | A centered system line such as "Chat started" |
| `Status` | `MokaChatMessageStatus` | `Sending`, `Sent`, `Delivered`, `Read` or `Error` |

## Basic Chat

```blazor-preview
@code {
    List<MokaChatMessage> _messages =
    [
        new() { Id = "1", Text = "Chat started", Author = "System", IsSystem = true },
        new() { Id = "2", Text = "Hey! How's the project going?", Author = "Alice", AvatarInitials = "AL" },
        new() { Id = "3", Text = "Almost done with the new components.", Author = "Me", IsOwn = true }
    ];

    void Send(string text) =>
        _messages = [.. _messages, new MokaChatMessage { Id = Guid.NewGuid().ToString("N"), Text = text, Author = "Me", IsOwn = true }];
}

<MokaChat Messages="_messages" OnSend="Send" Height="320px" />
```

## Keyboard

- Enter sends the message. Shift+Enter starts a new line.
- While an input method editor is composing (Chinese, Japanese or Korean input, for example), Enter confirms the composition and does not send.
