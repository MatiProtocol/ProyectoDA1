using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Services.DTOs;

namespace Services;

public class NotificationService
{
    private PureDatabase _database = PureDatabase.GetInstance();
    private ISqlContextFactory _contextFactory;
    private static NotificationService? _instance = null;

    public static NotificationService GetInstance(ISqlContextFactory? contextFactory = null)
    {
        if (_instance == null) _instance = new NotificationService();
        _instance._database =PureDatabase.GetNewInstance(contextFactory);
        return _instance;
    }
    
    public static NotificationService GetNewInstance(ISqlContextFactory? contextFactory = null)
    {
        var instance = new NotificationService();
        instance._database =PureDatabase.GetNewInstance(contextFactory);
        return instance;
    }

    private NotificationService(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        _contextFactory = contextFactory;
    }

    public (NotificationDTO[]? nots, Error? err) GetUserNotifications(UserDTO userDto)
    {
        var notifications = _database.Notifications.Query(context =>
            context.Notification.Where(n => n.UserId == userDto.Id)
            );

        if (notifications.Count() != 0) return (notifications.Select(NotificationDTO.ToEntity).ToArray(), null);

        var error = new Error("ArgumentError", "El usuario aun no tiene notificaciones");
        return (null, error);
    }

    public (NotificationDTO[]? nots, Error? err) GetUserPendingNotifications(UserDTO userDto)
    {
        var notifications = _database.Notifications.Query().AsEnumerable().Where((n) => n.UserToNotify.Email == userDto.Email);

        if (notifications.Count() != 0) return (notifications.Where(not => not.State == NotificationState.Pending).Select(NotificationDTO.ToEntity).ToArray(), null);

        var error = new Error("ArgumentError", "El usuario aun no tiene notificaciones");
        return (null, error);
    }


    public void SendNotification(User user, string message)
    {
       if (_database.Users.GetById(user.Id) == null) return;
       
        var notify = new Notification(user, message);
        
        _database.Notifications.Add(notify);
    }

    public void MarkAsRead(UserDTO userDto, NotificationDTO notificationDto)
    {
         if (_database.Users.FindBy(u => u.Email == userDto.Email) == null) return;
         
         var not = _database.Notifications.GetById(notificationDto.Id);
         if (not == null) return;
        
        not.State = NotificationState.Read;
        
         _database.Notifications.Update(not);
    }
}
