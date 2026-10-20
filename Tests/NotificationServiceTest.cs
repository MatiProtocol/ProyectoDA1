using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Services;
using Services.DTOs;

namespace Tests;


[TestClass]
public class NotificationServiceTest
{
    private int _i = 0;

    private ISqlContextFactory _contextFactory;
    private PureDatabase _database;
    private NotificationService _notificationService;

    private UserDTO _userDto = new ()
    {
        Name = "Richard",
        LastName = "Pepe",
        Email = "riki@mail.com",
        BirthDate = "10/04/2004",
        Password = "ContraPass1@",
    };
    private User _user = new ();
    private readonly UserDTO _invalidUserDto = new ()
    {
        Name = "Richard",
        LastName = "Pepe",
        Email = "riki@asjkdfjask.com",
        BirthDate = "10/04/2004",
        Password = "ContraPass1@",
    };

    [TestInitialize]
    public void Initialize()
    {
        _i += 1;
        _contextFactory = new InMemorySqlContextFactory("NotificationServiceTest" + _i);

        _notificationService = NotificationService.GetInstance(_contextFactory);
        _database = PureDatabase.GetNewInstance(_contextFactory);

        _user = _userDto.FromEntity(UserService.EncryptPassword("ContraPass1@")).Item1!;

        _database.Users.Add(_user);

        _userDto = UserService.GetNewInstance(_contextFactory).GetUserDto(_userDto.Email)!;
    }

    [TestCleanup]
    public void CleanUp()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void CreateNotificationService_NotNull()
    {
        Assert.IsNotNull(_notificationService);
    }

    [TestMethod]
    public void CreateNotification_NotNull()
    {
        Assert.IsNotNull(_notificationService);
    }

    [TestMethod]
    public void GetUserNotificationWithNotifications_ReturnError()
    {
        var err = _notificationService.GetUserNotifications(_userDto);

        Assert.IsInstanceOfType<Error>(err.Item2);
    }

    [TestMethod]
    public void SendGetUserNotificationWithNotifications_IsNull()
    {
        _notificationService.SendNotification(_user, "Hola");
        var err = _notificationService.GetUserNotifications(_userDto);

        Assert.IsNull(err.Item2);
    }

    [TestMethod]
    public void SendGetNotificationToNonExistentUser_ReturnError()
    {
        var userTest = new User("noname", "Locura", "pepp@ajasdk", DateTime.Now.AddYears(-20), [1, 3, 3, 8, 4]);

        _notificationService.SendNotification(userTest, "Hola");
        var err = _notificationService.GetUserNotifications(_userDto);

        Assert.IsInstanceOfType<Error>(err.Item2);
    }

    [TestMethod]
    public void SendGetFirstNotificationToExistentUser_IsNull()
    {

        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");
        var err = _notificationService.GetUserNotifications(_userDto);

        Assert.IsNull(err.Item2);
    }

    [TestMethod]
    public void SendGetFirstNotificationToExistentUser_Pending()
    {

        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");
        var (nots, _) = _notificationService.GetUserPendingNotifications(_userDto);

        Assert.AreEqual(2, nots!.Length);
    }

    [TestMethod]
    public void GetPendingNotification_InvalidUser()
    {

        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");
        var (_, err) = _notificationService.GetUserPendingNotifications(_invalidUserDto);

        Assert.IsNotNull(err);
    }

    [TestMethod]
    public void MarkAsRead_InvalidUser()
    {

        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");

        _notificationService.MarkAsRead(_invalidUserDto, new NotificationDTO(1, _userDto, "Hola", NotificationState.Pending));

        var (nots, _) = _notificationService.GetUserPendingNotifications(_userDto);

        Assert.AreEqual(2, nots!.Length);
    }

    [TestMethod]
    public void MarkAsRead_UserHasNoNotification()
    {

        _notificationService.MarkAsRead(_userDto, new NotificationDTO(1, _userDto, "Hola", NotificationState.Pending));

        var (nots, _) = _notificationService.GetUserPendingNotifications(_userDto);

        Assert.IsNull(nots);
    }

    [TestMethod]
    public void MarkAsRead_InvalidNotification()
    {
        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");

        _notificationService.MarkAsRead(_userDto, new NotificationDTO(8, _userDto, "Hola", NotificationState.Pending));

        var (nots, _) = _notificationService.GetUserPendingNotifications(_userDto);

        Assert.AreEqual(2, nots!.Length);
    }

    [TestMethod]
    public void MarkAsRead_ValidData()
    {
        _notificationService.SendNotification(_user, "Hola");
        _notificationService.SendNotification(_user, "ACK");
        var (n, _) = _notificationService.GetUserPendingNotifications(_userDto);

        _notificationService.MarkAsRead(_userDto, new NotificationDTO(n![0].Id, _userDto, "Hola", NotificationState.Pending));

        var (nots, _) = _notificationService.GetUserPendingNotifications(_userDto);

        Assert.AreEqual(1, nots!.Length);
    }
}
