using DBContext;
using Domain;

namespace DomainConstruct;

public class NotificationConstruct
{
    public static Notification Construct( Notification not, SqlContext context)
    {
        not.UserToNotify = context.Users.Find(not.UserId)!;
        return not;
    }
}