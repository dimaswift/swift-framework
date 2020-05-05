namespace SwiftFramework.Core
{
    public interface INotificationsManager : IModule
    {
        void Schedule(ILink notificationLink, long secondsDelay);
        void ScheduleDefault(string titleKey, string messageKey, long secondsDelay);
        void Cancel(ILink notificationLink);
        bool Enabled { get; set; }
    }
}
