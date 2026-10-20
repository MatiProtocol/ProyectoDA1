using Auxiliaries;
using Domain;

namespace Tests;


[TestClass]
public class NotificationTest
{
    
    private Notification _notification=new Notification();
    
    [TestInitialize]
    public void Initialize()
    {
    }
    
    [TestMethod]
    public void CreateNewNotification_NotNull()
    {
        Assert.IsNotNull(_notification);
    }

    [TestMethod]
    public void CreateNewNotificationWithParameters_NotNull()
    {
        Notification notif = new Notification(new User(), "Bienvenido");
        Assert.IsNotNull(notif);
    }
    
    [TestMethod]
    public void GetSetId_AssignCorrectly()
    {
        _notification.Id = 1;
        var id = _notification.Id;
        Assert.AreEqual(1, id);
    }
    
    [TestMethod]
    public void GetSetUserToNotify_AssignCorrectly()
    {
        _notification.UserToNotify = new User();
        var user = _notification.UserToNotify;
        Assert.IsNotNull(user);
    }
    
    [TestMethod]
    public void GetSetMessage_AssignCorrectly()
    {
        _notification.Message = "Bienvenido";
        var message = _notification.Message;
        Assert.AreEqual("Bienvenido", message);
    }
    
    [TestMethod]
    public void GetState_Default()
    {
        Assert.AreEqual(NotificationState.Pending, _notification.State);
    }
    
    [TestMethod]
    public void GetSetState_AssignCorrectly()
    {
        _notification.State = NotificationState.Read;
        Assert.AreEqual(NotificationState.Read, _notification.State);
    }
}