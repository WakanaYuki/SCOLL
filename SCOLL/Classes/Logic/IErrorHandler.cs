using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCOLL.Classes.Logic
{
    public interface IErrorHandler
    {
        void ShowNotification(NotificationType notificationType, string title, string message, int duration = 10, bool taskbar = false);
    }
}
