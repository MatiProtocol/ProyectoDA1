using Auxiliaries;
using Domain;

namespace Services.DTOs;

public class NotificationDTO(int id, UserDTO userDto, string message, NotificationState state)
{
    public int Id = id;
    public UserDTO UserDtoToNotify = userDto;
    public string Message = message;
    public NotificationState State = state;

    public static NotificationDTO ToEntity(Notification notification)
    {
        return new NotificationDTO(notification.Id, UserDTO.ToEntity(notification.UserToNotify), notification.Message, notification.State);
    }
    
    public (Notification? not ,Error? err) FromEntity()
    {
        var (user, err) = UserDtoToNotify.FromEntity([]);
        if(err != null) return (null, err);
        return (new Notification
        {
            Id = Id,
            UserToNotify = user!,
            Message = Message,
            State = State,
        }, null);
    }
}