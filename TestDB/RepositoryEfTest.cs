using System.Linq.Expressions;
using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Microsoft.IdentityModel.Tokens;

namespace TestDB;

[TestClass]
public class RepositoryEfTest
{
    private RepositoryEf<User, int> _repository = null!;
    private ISqlContextFactory _contextFactory = null!;
    private int _i = 0;

    [TestInitialize]
    public void Setup()
    {
        _i++;
        _contextFactory = new InMemorySqlContextFactory("RepositoryEfTest" + _i);
        _repository = new RepositoryEf<User, int>(_contextFactory, UniqueEmailRule, GetKeyValue); ;
        return;

        Expression<Func<User, bool>> UniqueEmailRule(User user) => u => u.Email == user.Email && u.Id != user.Id;
        int GetKeyValue(User user) => user.Id;
    }

    [TestCleanup]
    public void CleanUp()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void AddUser_AddUserCorrectly()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var error = _repository.Add(user);

        Assert.IsNull(error);
    }

    [TestMethod]
    public void AddUserWithTrackEmail_ReturnError()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);
        user.Id = 0;
        var error = _repository.Add(user);

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void AddUserWithTrackId_ReturnError()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);
        var error = _repository.Add(user);

        Assert.IsInstanceOfType<Error>(error);
    }


    [TestMethod]
    public void FindById_ReturnUser()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var result = _repository.FindBy(u => u.Email == user.Email);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void FindByIdUserNonTrackEmail_ReturnNull()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var result = _repository.FindBy(u => u.Email == user.Email);

        Assert.IsNull(result);
    }


    [TestMethod]
    public void DeleteUserTrackEmail_ReturnTrue()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var userAux = _repository.FindBy(u => u.Email == user.Email);
        if (userAux == null) return;

        var deleted = _repository.Remove(user.Id);

        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void GetById_ReturnUser()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var result = _repository.GetById(1);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetById_ReturnNull()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var result = _repository.GetById(1);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetAllWithEmptyUsers_ReturnNull()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var result = _repository.GetAll();

        Assert.IsTrue(result.IsNullOrEmpty());
    }

    [TestMethod]
    public void GetAllWithUsers_ReturnUsers()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var result = _repository.GetAll();

        Assert.IsTrue(result.Count == 1);
    }

    [TestMethod]
    public void DeleteUserNonTrackEmail_ReturnFalse()
    {
        var deleted = _repository.Remove(2);

        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public void UpdateWithNonTrackUser_ReturnError()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var result = _repository.Update(user);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void UpdateUser_UpdateCorrectly()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var userAux = _repository.FindBy(u => u.Email == user.Email);
        if (userAux == null) return;

        userAux.Email = "pepinillo@fresca.com";
        _repository.Update(userAux);

        var newUserInfo = _repository.FindBy(u => u.Email == user.Email);
        if (newUserInfo == null) return;

        Assert.AreEqual(newUserInfo.Email, user.Email);
    }

    [TestMethod]
    public void ExistWithTrackUser_ReturnTrue()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var result = _repository.Exists(u => u.Email == user.Email);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ExistWithNonTrackUser_ReturnTrue()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        var result = _repository.Exists(u => u.Email == user.Email);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetContext()
    {
        var context = _repository.GetContext();
        Assert.IsNotNull(context);
    }

    [TestMethod]
    public void Query_ReturnSome()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);

        var result = _repository.Query().Select(u => u.Email).ToList();

        Assert.IsTrue(result.Count > 0 && result.Contains(user.Email));
    }

    [TestMethod]
    public void QueryWithContext_ReturnSome()
    {
        var user = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };
        
        var user1 = new User
        {
            Name = "tata",
            LastName = "tata",
            Email = "tata@tatita1.com",
            BirthDate = new DateTime(2004, 04, 10),
            Password = [1, 2, 3, 4]
        };

        _repository.Add(user);
        _repository.Add(user1);

        var result = _repository.Query((context) => context.Users.Where(u => u.Email == "tata@tatita1.com")).Select(u => u.Email).ToList();
        Assert.IsTrue(result.Count > 0 && result.Contains(user1.Email));
    }

}
