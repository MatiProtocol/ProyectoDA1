using Auxiliaries;

namespace Domain;

public class Notification
{
    private int _id;
    private int _userId;
    private string _message;
    private User _userToNotify;
    private NotificationState _state = NotificationState.Pending;

    public Notification() {}
    
    public Notification(User user, string message)
    {
        UserToNotify = user;
        Message = message;    
        UserId = user.Id;
    }
    
    public int Id
    {
        get => _id;
        set => _id = value;
    }
    
    public NotificationState State
    {
        get => _state;
        set => _state = value;
    }
    
    public int UserId
    {
        get => _userId;
        set => _userId= value;
    }
    
    public User UserToNotify
    {
        get => _userToNotify;
        set => _userToNotify = value;
    }
    
    public string Message
    {
        get => _message;
        set => _message = value;
    }
}