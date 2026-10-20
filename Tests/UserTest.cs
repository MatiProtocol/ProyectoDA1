using System.Globalization;
using Domain;
using Auxiliaries;

namespace Tests;

[TestClass]
public class UserTest
{
    private User _user = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        byte[] bytePass = [3, 4, 2, 1, 0, 1];
        _user = new User("Gerson", "Barreiro", "Gersiño@quepelota.gol", DateTime.Parse("10/12/1959", CultureInfo.CreateSpecificCulture("en-US")), bytePass);
    }

    [TestMethod]
    public void CreateUser_IsNotNull()
    {
        Assert.IsNotNull(_user);
    }

    [TestMethod]
    public void CreateUserWithInvalidEmail_ThrowArgumentException()
    {
        byte[] byteTest = { 3, 4, 2, 1, 0 };
        
        Assert.ThrowsException<ArgumentException>(() =>
            new User("Gerson", "Barreiro", "Gersiñogol", DateTime.Parse( "10/12/1959", CultureInfo.CreateSpecificCulture("en-US")), byteTest));
    }

    [TestMethod]
    public void CreateUserWithInvalidBirthDate_ThrowArgumentException()
    {
        byte[] byteTest = { 3, 4, 2, 1, 0 };

        Assert.ThrowsException<FormatException>(() =>
            new User("Gerson", "Barreiro", "Gersiño@quepelota.gol", DateTime.Parse("15/15/2002", CultureInfo.CreateSpecificCulture("en-US")), byteTest));
    }

    [TestMethod]
    public void SetGetName_AssignNameCorrectly()
    {
        var newName = "Fiumba";
        
        _user.Name = newName;
        var result = _user.Name;
        
        Assert.AreEqual(newName, result);
    }

    [TestMethod]
    public void SetGetLastName_AssignLastNameCorrectly()
    {
        var newLastName = "kiki";
        
        _user.LastName = newLastName;
        var result = _user.LastName;
        
        Assert.AreEqual(newLastName, result);
    }

    [TestMethod]
    public void SetGetRole_AssignRoleCorrectly()
    {
        var role = Roles.ProjectManager;
        
        _user.Role = role;
        var result = _user.Role;
        
        Assert.AreEqual(role, result);
    }

    [TestMethod]
    public void SetEmailWithWrongFormat_ThrowException()
    {
        var wrongEmail = "piripichi";
        
        Assert.ThrowsException<ArgumentException>(() => _user.Email = wrongEmail);
    }

    [TestMethod]
    public void SetEmailWithWrongFormat_CorrectMessage()
    {
        var wrongEmail = "piripichi";

        try
        {
            _user.Email = wrongEmail;
        }
        catch (ArgumentException e)
        {
            Assert.AreEqual(e.Message, "Invalid email format");
        }
    }

    [TestMethod]
    public void SetGetEmail_AssignEmailCorrectly()
    {
        var correctEmail = "piripichi@gmail.com";

        _user.Email = correctEmail;
        
        Assert.AreEqual(correctEmail, _user.Email);
    }

    [TestMethod]
    public void GetSetId_ReturnId()
    {
        _user.Id = 1;
        var result = _user.Id;
        Assert.AreEqual(1, result);
    }
    
    [TestMethod]
    public void SetBirthDateWithUnderageDate_ThrowError()
    {
        var underageDate = DateTime.Today.AddDays(-17).AddDays(1);
        
        Assert.ThrowsException<ArgumentException>(() => _user.BirthDate = underageDate);
    }

    [TestMethod]
    public void SetBirthDateWithUnderageDate_CorrectMessage()
    {
        var exampleDate = DateTime.Parse(DateTime.Today.AddDays(1).ToString("MM/dd/yyyy"),
            CultureInfo.CreateSpecificCulture("en-US"));

        try
        {
            _user.BirthDate = exampleDate;
        }
        catch (ArgumentException e)
        {
            Assert.AreEqual(e.Message, "User must be at least 18 years old");
        }
    }

    [TestMethod]
    public void SetGetBirthDate_AssignBirthDateCorrectly()
    {
        var newBirthDate = "03/22/2002";

        _user.BirthDate = DateTime.Parse(newBirthDate, CultureInfo.CreateSpecificCulture("en-US"));
        var result = _user.BirthDate.ToString("MM/dd/yyyy",CultureInfo.CreateSpecificCulture("en-US"));

        Assert.AreEqual(newBirthDate, result);
    }

    [TestMethod]
    public void ObtainUserRole_GetCorrectUserRole()
    {
        var expectedRole = Roles.ProjectMember;
        
        var result = _user.Role;
        
        Assert.AreEqual(expectedRole, result);
    }

    [TestMethod]
    public void ComparePasswordWithCorrectlyPass_IsTrue()
    {
        byte[] byteTest = { 1, 2, 3 };
        _user.Password = byteTest;
        
        var result = _user.Password.SequenceEqual(byteTest);
        
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GetPasswordCompareSequenceCorrectly_IsFalse()
    {
        byte[] byteTest = { 1, 2, 3 };

        var result = _user.Password.SequenceEqual(byteTest);
        
        Assert.IsFalse(result);
    }
    
    [TestMethod]
    public void EmptyConstructor_createUserCorrectly()
    {
        var user = new User();
        Assert.IsNotNull(user);
    }
}
