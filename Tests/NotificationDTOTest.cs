using Auxiliaries;
using Domain;
using Services.DTOs;

namespace Tests;

[TestClass]
public class NotificationDTOTest
{
    private static User _user = new User("pep", "pe", "jj@jj", DateTime.Now.AddYears(-20), [1, 3, 3, 8, 4]);
    private static Notification _notification = new Notification(_user, "Jota");
    private static NotificationDTO _notificationDto = new NotificationDTO(0, UserDTO.ToEntity(_user),"Jota", NotificationState.Pending);

    [TestMethod]
    public void ConstructorTest()
    {
        Assert.IsNotNull(new NotificationDTO(1, UserDTO.ToEntity(_user), "Jota", NotificationState.Pending));
    }
    
    [TestMethod]
    public void ConstructorCorrectMessage()
    {
        var not = new NotificationDTO(1, UserDTO.ToEntity(_user), "Jota", NotificationState.Pending);
        
        Assert.AreEqual("Jota", not.Message);
    }

    [TestMethod]
    public void ToEntity_EmailCorrect()
    {
        
        Assert.AreEqual(_notificationDto.UserDtoToNotify.Email, NotificationDTO.ToEntity(_notification).UserDtoToNotify.Email);
    }
    
    [TestMethod]
    public void ToEntity_IdCorrect()
    {
        
        Assert.AreEqual(_notificationDto.Id, NotificationDTO.ToEntity(_notification).Id);
    }
    
    [TestMethod]
    public void ToEntity_MessageCorrect()
    {
        
        Assert.AreEqual(_notificationDto.Message, NotificationDTO.ToEntity(_notification).Message);
    }
    
    [TestMethod]
    public void ToEntity_StateCorrect()
    {
        
        Assert.AreEqual(_notificationDto.State, NotificationDTO.ToEntity(_notification).State);
    }
    
    [TestMethod]
    public void GenerateValidNotification_FromEntity()
    {
        Assert.AreEqual(_notification.Id, _notificationDto.FromEntity().not!.Id);
    }
    
    [TestMethod]
    public void GenerateValidMessage_FromEntity()
    {
        Assert.AreEqual(_notification.Message, _notificationDto.FromEntity().not!.Message);
    }
    
    [TestMethod]
    public void GenerateValidState_FromEntity()
    {
        Assert.AreEqual(NotificationState.Pending, _notificationDto.FromEntity().not!.State);
    }
    
    [TestMethod]
    public void FromEntityWithInvalidUserDtoMail()
    {
        _notificationDto.UserDtoToNotify.Email = "p";
        Assert.IsNotNull(_notificationDto.FromEntity().err);
    }
}