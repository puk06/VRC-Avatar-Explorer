using System;
using Avalonia.Controls.Notifications;
using Message.Avalonia;
using Message.Avalonia.Models;

namespace AvatarExplorer.UI.Services.System;

public static class NotificationManager
{
    public static void Show(string title, string content, NotificationType type)
    {
        var manager = MessageManager.Default;
        var messageOptions = new MessageOptions
        {
            Title = title,
            Duration = TimeSpan.FromSeconds(3.5)
        };

        switch (type)
        {
            case NotificationType.Information:
                manager.ShowInformationMessage(content, messageOptions);
                break;
            case NotificationType.Success:
                manager.ShowSuccessMessage(content, messageOptions);
                break;
            case NotificationType.Warning:
                manager.ShowWarningMessage(content, messageOptions);
                break;
            case NotificationType.Error:
                manager.ShowErrorMessage(content, messageOptions);
                break;
        }
    }
}
