using System.Linq.Expressions;
using Auxiliaries;
using Controllers.DBControllers;
using DataAccess;
using DBContext;
using Domain;
using Microsoft.IdentityModel.Tokens;
using Services.DTOs;

namespace TestDB;


[TestClass]
public class UserControllerTests
{
    private RepositoryEf<User, int> repository = null!;
    private UserController _controller = null!;
    private ISqlContextFactory _contextFactory = null!;
    private int _i = 0;
    [TestInitialize]
    public void Setup()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("UserControllerTests" +_i);

        repository = new RepositoryEf<User, int>(_contextFactory, UniqueEmailRule, GetKeyValue);
        _controller = new UserController(repository);
        return;

        Expression<Func<User, bool>> UniqueEmailRule(User user) => u => u.Email == user.Email && u.Id != user.Id;
        int GetKeyValue(User user) => user.Id;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void AddUser_AddUserCorrectly()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };

        var error = _controller.Add(userDto);

        Assert.IsNull(error);
    }
    
    [TestMethod]
    public void AddUserWithTrackEmail_ReturnError()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };

        _controller.Add(userDto);
        var error = _controller.Add(userDto);

        Assert.IsInstanceOfType<Error>(error);
    }
    
    
    [TestMethod]
    public void FindById_ReturnUser()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };

        _controller.Add(userDto);
        
        var result = _controller.FindBy(u => u.Email == userDto.Email);

        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void FindByIdUserNonTrackEmail_ReturnNull()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        var result = _controller.FindBy(u => u.Email == userDto.Email);

        Assert.IsNull(result);
    }
    
    
    [TestMethod]
    public void DeleteUserTrackEmail_ReturnTrue()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };

        _controller.Add(userDto);
        
        var user = _controller.FindBy(u => u.Email == userDto.Email);
        if (user == null) return;
        
        var deleted = _controller.Remove(user.Id);

        Assert.IsTrue(deleted);
    }
    
    [TestMethod]
    public void GetById_ReturnUser()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };

        _controller.Add(userDto);
        
        var result = _controller.GetById(1);
        
        Assert.IsNotNull(result);
    }
    
    [TestMethod]
    public void GetById_ReturnNull()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        var result = _controller.GetById(1);
        
        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void GetAllWithEmptyUsers_ReturnNull()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        var result = _controller.GetAll();
        
        Assert.IsTrue(result.IsNullOrEmpty());
    }
    
    [TestMethod]
    public void GetAllWithUsers_ReturnUsers()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        _controller.Add(userDto);
        
        var result = _controller.GetAll();
        
        Assert.IsTrue(result.Count == 1);
    }
    
    [TestMethod]
    public void DeleteUserNonTrackEmail_ReturnFalse()
    {
        var deleted = _controller.Remove(2);

        Assert.IsFalse(deleted);
    }
    
    [TestMethod]
    public void UpdateWithNonTrackUser_ReturnError()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        _controller.Add(userDto);
        var user =_controller.FindBy(u => u.Email == userDto.Email);
        if (user == null) return;
        user.Email = "sacachispas@mail.com";
        user.Id = 87;
        var result = _controller.Update(UserDTO.ToEntity(user));
        
        Assert.IsInstanceOfType<Error>(result);
    }
    
    [TestMethod]
    public void UpdateUser_UpdateCorrectly()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        _controller.Add(userDto);
        var user =_controller.FindBy(u => u.Email == userDto.Email);
        if (user == null) return;

        userDto = UserDTO.ToEntity(user);
        
        userDto.Email = "Pepinillo@fresca.com";
        _controller.Update(userDto);
        
        var newUserInfo =_controller.FindBy(u => u.Email == userDto.Email);
        if (newUserInfo == null) return;
        userDto.Email = "tata@tatita.com";
        
        Assert.AreNotEqual(newUserInfo.Email, userDto.Email);
    }
    
    [TestMethod]
    public void ExistWithTrackUser_ReturnTrue()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        _controller.Add(userDto);
        
        var result = _controller.Exists(u => u.Email == userDto.Email);
        
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void ExistWithNonTrackUser_ReturnTrue()
    {
        var userDto = new UserDTO
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = "10/04/2004"
        };
        
        var result = _controller.Exists(u => u.Email == userDto.Email);
        
        Assert.IsFalse(result);
    }
}