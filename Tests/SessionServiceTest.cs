using Auxiliaries;
using DBContext;
using Services;
using Services.SeederDB;

namespace Tests;

[TestClass]
public class SessionServiceTest
{
    private SessionService _session = null!;
    private ISqlContextFactory _contextFactory;
    private int _i = 0;

    [TestInitialize]
    public void Init()
    {
        _i += 1;
        _contextFactory = new InMemorySqlContextFactory("SessionServiceTest" + _i);

        SeedAll.Seed(_contextFactory);

        _session = SessionService.GetInstance(_contextFactory);
    }

    [TestCleanup]
    public void CleanUp()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void CreateSession_NotNull()
    {
        Assert.IsNotNull(_session);
    }
    
    [TestMethod]
    public void GetNewInstance_NotNull()
    {
        var session = SessionService.GetNewInstance();
        Assert.IsNotNull(session);
    }
    
    [TestMethod]
    public void Login_WhenFirstLoginKeepThatUser()
    {
        var mail = "root@root.com";
        _session.Login(mail, "PassRoot1@");
        Assert.AreEqual(mail, _session.GetCurrent()?.Email);
    }

    [TestMethod]
    public void Login_SecondLoginMustReturnError()
    {
        var mail = "root@root.com";
        var pass = "PassRoot1@";
        _session.Login(mail, pass);

        Assert.AreEqual("AlreadyLogged", _session.Login(mail, pass)?.GetType());
    }

    [TestMethod]
    public void Login_Password_ErrorTypeAlreadyLogged()
    {
        var mail = "root@root.com";
        var pass = "PassRoot1@";
        Assert.AreEqual("AlreadyLogged", _session.Login(mail, pass)?.GetType());
    }

    [TestMethod]
    public void Login_User_ErrorTypeAlreadyLogged()
    {
        var mail = "root@ot.com";
        var pass = "PassRoot1@";
        Assert.AreEqual("AlreadyLogged", _session.Login(mail, pass)?.GetType());
    }

    [TestMethod]
    public void LoginUserNotTrackingUser_returnError()
    {
        var error = _session.Login("Example@wrong.com", "PassRoot1@");
        Assert.IsInstanceOfType<Error?>(error);
    }

    [TestMethod]
    public void LoginUserWithWrongPassword_returnError() => Assert.IsInstanceOfType<Error?>(_session.Login("root@root.com", "PassRoot1@@"));

    [TestMethod]
    public void Logout_CurrentNull()
    {
        var mail = "root@root.com";
        var pass = "PassRoot1@";
        _session.Login(mail, pass);

        _session.Logout();

        Assert.IsNull(_session.GetCurrent());
    }
    
    [TestMethod]
    public void LoginWithEmailNotTracked_ReturnError()
    {
        var mail = "rootonnn@root.com";
        var pass = "PassRoot1@";
        var error = _session.Login(mail, pass);

        _session.Logout();

        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void LoginWithWrongPassword_ReturnError()
    {
        var mail = "root@root.com";
        var pass = "que contra mas mala";
        var error = _session.Login(mail, pass);
        
        Assert.IsInstanceOfType<Error>(error);
    }
}
