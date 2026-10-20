using DataAccess;
using DBContext;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Services.SeederDB;

public abstract class NotificationSeederDb : ISeeder
{
    public static void Seed(ISqlContextFactory contextFactory)
    {
        var context = contextFactory.Generate();
        if (!context.Users.Any() || context.Notification.Any()) return;

        var rootUser = context.Users.FirstOrDefault(u => u.Email == "root@root.com");
        
        var notification = new Notification
        {
            UserToNotify = rootUser!,
            Message = "Bienvenido a Task Pro por primera vez",
            UserId = rootUser!.Id
        };
        
        context.Notification.Add(notification);
        context.SaveChanges();
    }
}