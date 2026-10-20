using DBContext;
using Domain;
using Services.SeederDB;

namespace TestDB.SeedersTest;
    
[TestClass]
public class NotificationSeederDbTests
{
    private ISqlContextFactory _contextFactory;
    private int _i = 0;
    
    [TestInitialize]
    public void Initialize()
    {
        _i++;
        _contextFactory =new InMemorySqlContextFactory("NotificationSeederDbTests" + _i);

        var rootUser = new User("root", "", "root@root.com", new DateTime(1998, 3, 12), new byte[32]);
        var _context = _contextFactory.Generate();
        _context.Users.Add(rootUser);
        _context.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void Seed_CreateNotificationForRootUser()
    {
        NotificationSeederDb.Seed(_contextFactory);

        var context = _contextFactory.Generate();
        var notification = context.Notification.FirstOrDefault();
        Assert.IsNotNull(notification);
    }

    [TestMethod]
    public void SeedWhenUserNotExists_NotCreateNotification()
    {
        var context = _contextFactory.Generate();
        context.Users.RemoveRange(context.Users);
        context.SaveChanges();

        NotificationSeederDb.Seed(_contextFactory);

        Assert.AreEqual(0, context.Notification.Count());
    }

    [TestMethod]
    public void SeedWhenAlreadyExists_NotCreateNotification()
    {
        var context = _contextFactory.Generate();
        var rootUser = context.Users.First();
        var existing = new Notification
        {
            UserToNotify = rootUser,
            Message = "Ya existe"
        };
        
        context.Notification.Add(existing);
        context.SaveChanges();

        NotificationSeederDb.Seed(_contextFactory);

        Assert.AreEqual(1, context.Notification.Count());
    }
}
