using System.Globalization;
using DataAccess;
using Auxiliaries;
using DBContext;
using Domain;
using Domain.HelpersDB;
using Services;
using Services.DTOs;

namespace Tests;

[TestClass]
public class UserServiceTest
{

    private static ISqlContextFactory _contextFactory;
    
    private UserService _userService = null!;
    private PureDatabase _database;
    private UserDTO _userDto = null!;
    private UserDTO? _systemManager;

    private int _i = 0;
    [TestInitialize]
    public void TestInitialize()
    {
        _i += 1;
        _contextFactory = new  InMemorySqlContextFactory("UserServiceTest" + _i);
        _userService = UserService.GetNewInstance(_contextFactory);
        _database = PureDatabase.GetNewInstance(_contextFactory);
            
        _userDto = new UserDTO
        {
            Name = "",
            LastName = "",
            Email = "test@test.com",
            BirthDate = "04/18/2002",
            Password = "123456Ac@"
        };
        
        _systemManager = new UserDTO
        {
            Name = "root",
            LastName = "",
            Email = "root@root.com",
            BirthDate = "04/18/2002",
            Password = "PassRoot1@",
            Role = Roles.SystemManager
        };
        
        _database.Users.Add(_systemManager.FromEntity([0,1,2,3]).Item1!);
        if (_systemManager != null) _userService.CreateUser(_systemManager, _userDto);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _contextFactory.Generate().Database.EnsureDeleted();
    }

    [TestMethod]
    public void CreateUser_NotEnoughPermission()
    {
        var user = _database.Users.FindBy(u => u.Email.Equals("root@root.com"));
        user!.Role = Roles.ProjectMember;
        _database.Users.Update(user);

        var newUser = new UserDTO
        {
            Name = "Momo",
            LastName = "Relojero",
            BirthDate = "11/04/1989",
            Email = "Momo@Ladinastia.com",
            Password = "123456Ac@A!"
        };
        
        if (_systemManager == null) return;
        var result = _userService.CreateUser(_systemManager, newUser);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreateUserService_isNotNull()
    {
        Assert.IsNotNull(_userService);
    }
    
    [TestMethod]
    public void CreateUserServiceGetInstanceWithContext_isNotNull()
    {
        var userService = UserService.GetInstance(_contextFactory);
        Assert.IsNotNull(userService);
    }
    
    [TestMethod]
    public void CreateUserServiceGetNewInstanceWithout_isNotNull()
    {
        var userService = UserService.GetNewInstance();
        Assert.IsNotNull(userService);
    }

    [TestMethod]
    public void CreateUserInvalidEmail_returnError()
    {
        //Arrange
        _userDto.Email = "testtest.com";
        var systemManager = _userService.GetUserDto("root@root.com");

        if (systemManager == null) return;
        var result = _userService.CreateUser(systemManager, _userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreateUserInvalidBirthDate_returnError()
    {
        //Arrange
        _userDto.Email = "test2@test.com";
        _userDto.BirthDate = "34";
        var systemManager = _userService.GetUserDto("root@root.com");

        if (systemManager == null) return;
        var result = _userService.CreateUser(systemManager, _userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreateUserWithReapetedEmail_MustReturnError()
    {
        //Arrange
        var systemManager = _userService.GetUserDto("root@root.com");
        var userDto = new UserDTO()
        {
            Name = "tata",
            LastName = "tata",
            Email = "test@test.com",
            BirthDate = "10/04/2004",
            Password = "123456Ac@A!",
        };

        if (systemManager == null) return;
        var result = _userService.CreateUser(systemManager, userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreateUserInvalidPassword_returnError()
    {
        //Arrange
        _userDto.Email = "test2@test.com";
        _userDto.Password = "123456";

        if (_systemManager == null) return;
        var result = _userService.CreateUser(_systemManager, _userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreatUserWithCorrectlyParameters_returnNull()
    {
        _userDto.Email = "test2@test.com";

        if (_systemManager == null) return;
        var result = _userService.CreateUser(_systemManager, _userDto);
        //Arrange
        Assert.IsNull(result);
    }

    [TestMethod]
    public void CreateUserWIthInvalidPassword_ThrowArgumentException()
    {
        //Arrange
        _userDto.Password = "<PASSWORD>";
        _userDto.Email = "Tetriz@hotmail.com";

        if (_systemManager == null) return;
        var result = _userService.CreateUser(_systemManager, _userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void CreateUserWithOutSystemManagerRole_returnError()
    {
        //Arrange
        _userDto.Password = "<PASSWp23@RD>";
        _userDto.Email = "Tetriz@hotmail.com";
        if (_systemManager != null) _systemManager.Role = Roles.ProjectMember;

        var result = _userService.CreateUser(_userDto, _userDto);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void DeleteUserWithAnUnTrackEmail_returnFalse()
    {

        _userDto.Email = "sdjfka@skd";

        var result = _userService.DeleteUser(_systemManager!, _userDto);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DeleteUserWithAnValidEmail_ShouldDeleteUser()
    {
        //Arrange
        _userService.DeleteUser(_systemManager!, _userDto);

        var result = _userService.GetUserDto(_userDto.Email);

        Assert.IsNull(result);
    }
    
    [TestMethod]
    public void DeleteUserWithAnValidEmail_ButIsManager()
    {
        //Arrange
        _userService.ChangeRole(_systemManager!, _userDto.Email, Roles.ProjectManager);
        var projectDto = new ProjectDTO()
        {
            Name = "Pepe",
            Description = " ",
        };
        ProjectService.GetNewInstance(_contextFactory).CreateProject(_userDto, projectDto);
        ProjectService.GetNewInstance(_contextFactory).AddManagerToProject(_systemManager, _userDto, projectDto);
        
        var result = _userService.DeleteUser(_systemManager!, _userDto);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DeleteUserNoEnoughPermission_ShouldFail()
    {
        if (_systemManager == null) return;
        
        var user = _database.Users.FindBy(u => u.Email.Equals(_systemManager.Email));
        user!.Role = Roles.ProjectManager;
        
        _database.Users.Update(user);

        var result = _userService.DeleteUser(_systemManager!, _userDto);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void DeleteUserASystemManager_NotPossible()
    {
        var result = _userService.DeleteUser(_systemManager!, _systemManager!);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetUserInfoWithExistingUser_ReturnUserInfo()
    {
        var userDto = _userService.GetUserDto(_userDto.Email);

        Assert.IsInstanceOfType<UserDTO>(userDto);
    }

    [TestMethod]
    public void ChangeEmailWithCorrectlyEmail_returnNull()
    {
        var result = _userService.ChangeEmail(_userDto.Email, "test4@test.com");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ChangeEmailWithUsedEmail_MustFail()
    {
        var result = _userService.ChangeEmail(_userDto.Email, _systemManager!.Email);

        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void ChangeEmailWithoutOldEmailValidValue_returnError()
    {
        var result = _userService.ChangeEmail("test2@test.com", "test3@test.com");

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeEmailWithNewInvalidEmail_returnError()
    {
        var result = _userService.ChangeEmail("test@test.com", "testtest.com");

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void VerifyPasswordWithValidPassword_returnTrue()
    {
        bool result = _userService.VerifyPassword(_userDto, "123456Ac@");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyPasswordWithInvalidPassword_returnFalse()
    {

        bool result = _userService.VerifyPassword(_userDto, "123456");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPasswordWithNonTrackEmail_returnFalse()
    {
        UserDTO externalDto = new UserDTO();
        externalDto.Email = "test2@test.com";

        bool result = _userService.VerifyPassword(externalDto, "123456");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ChangePasswordWithWrongPassword_returnFalse()
    {
        _userDto.Password = "<PASSWO2@sRD>";

        bool result = _userService.ChangePassword(_userDto, "<PASSWO2@sRD>", "123456@As");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ChangePasswordWithNewWrongPassword_returnFalse()
    {
        _userDto.Password = "123456Ac@";

        bool result = _userService.ChangePassword(_userDto, "123456Ac@", "sas");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ChangePasswordWithValidPassword_returnTrue()
    {
        var result = _userService.ChangePassword(_userDto, "123456Ac@", "klklklkl56@As");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ChangeRoleToInvalidUser()
    {
        _userDto.Email = "aijsdk@jksdjfla";

        if (_systemManager == null) return;
        var result = _userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectManager);
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRoleWithoutRole_ReturnError()
    {
        if (_systemManager == null) return;
        var user = _database.Users.FindBy(u => u.Email.Equals(_systemManager.Email));
        user!.Role = Roles.ProjectManager;
        
        _database.Users.Update(user);

        var result = _userService.ChangeRole(_systemManager, _systemManager.Email, Roles.ProjectManager);
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRoleWithNonTrackEmail_ReturnError()
    {
        if (_systemManager == null) return;
        _systemManager.Email = "piripichi@sd.com";

        var result = _userService.ChangeRole(_systemManager, _systemManager.Email, Roles.ProjectManager);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRole_ShouldChangeRole()
    {
        if (_systemManager == null) return;
        _userService.ChangeRole(_systemManager, _userDto.Email, Roles.SystemManager);

        var result = _userService.GetUserDto(_userDto.Email)?.Role;
        
        Assert.AreEqual(Roles.SystemManager, result);
    }

    [TestMethod]
    public void ChangeRoleWithUserSystemManagerToLowerRole_ReturnError()
    {
        if (_systemManager == null) return;

        var result = _userService.ChangeRole(_systemManager, _systemManager.Email, Roles.SystemManager);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRoleToInvalidUser_UsingRolesInstradOfString()
    {
        _userDto.Email = "aijsdk@jksdjfla";

        if (_systemManager == null) return;
        var result = _userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectManager);
        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRoleWithoutRole_ReturnError_UsingRolInsteadOfString()
    {
        if (_systemManager == null) return;
        var user = _database.Users.FindBy(u => u.Email.Equals(_systemManager.Email));
        user!.Role = Roles.ProjectManager;
        
        _database.Users.Update(user);

        Assert.IsInstanceOfType<Error>(_userService.ChangeRole(_systemManager, _systemManager.Email,
            Roles.SystemManager));
    }

    [TestMethod]
    public void ChangeRoleWithNonTrackEmail_ReturnError_UsingRolInsteadOfString()
    {
        if (_systemManager == null) return;
        _systemManager.Email = "piripichi@sd.com";
        Assert.IsInstanceOfType<Error>(_userService.ChangeRole(_systemManager, _systemManager.Email,
            Roles.SystemManager));
    }

    [TestMethod]
    public void ChangeRoleLeaderLeadProjectToLowerRole_returnError()
    {

        var projectDto = new ProjectDTO
        {
            Name = "Proyiuto",
            Description = "Descricricri"
        };

        var projectService = ProjectService.GetNewInstance(_contextFactory);
        projectService.CreateProject(_systemManager!, projectDto);
        
        _userService.ChangeRole(_systemManager!, _userDto.Email, Roles.ProjectLeader);
        
        projectService.AddManagerToProject(_systemManager!, _systemManager!, projectDto);
        projectService.SetLeaderToProject(_systemManager!, _userDto, projectDto);

        var result = _userService.ChangeRole(_systemManager!, _userDto.Email, Roles.ProjectMember);

        Assert.IsInstanceOfType<Error>(result);
    }

    [TestMethod]
    public void ChangeRoleProjectManagerManageProjectToLowerRole_returnError()
    {
        if (_systemManager == null) return;

        var projectDto = new ProjectDTO
        {
            Name = "Proyiuto",
            Description = "Descricricri"
        };

        var projectService = ProjectService.GetNewInstance(_contextFactory);
        projectService.CreateProject(_systemManager, projectDto);
        _userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectManager);
        projectService.AddManagerToProject(_systemManager, _userDto, projectDto);

        var result = _userService.ChangeRole(_systemManager, _userDto.Email, Roles.ProjectMember);

        Assert.IsInstanceOfType<Error>(result);
    }


    [TestMethod]
    public void UserArrayToUserDto()
    {
        var u1 = new User("10", "10", "p@p", DateTime.Parse("10/9/1900", CultureInfo.CreateSpecificCulture("en-US")), new byte[] { 1, 2, 3 });
        var u2 = new User("12", "12", "a@b", DateTime.Parse("10/9/1900", CultureInfo.CreateSpecificCulture("en-US")), new byte[] { 1, 2, 3 });
        var u3 = new User("13", "13", "b@b", DateTime.Parse("10/9/1900", CultureInfo.CreateSpecificCulture("en-US")), new byte[] { 1, 2, 3 });
        var userArray = new[] { u1, u2, u3 };

        var dtoArray = _userService.UserArrayToDto(userArray);

        Assert.AreEqual(3, dtoArray.Length);
    }

    [TestMethod]
    public void GetAllUserDto()
    {
        var u1 = new User("10", "10", "p@p", DateTime.Parse("10/9/1900", CultureInfo.CreateSpecificCulture("en-US")), new byte[] { 1, 2, 3 });
        u1.Role = Roles.ProjectManager;
        
        _database.Users.Add(u1);
        
        var result = _userService.GetAllUserDto();

        Assert.AreEqual(3, result.Length);
    }

    [TestMethod]
    public void GetAllUserWhichCanAdminNewProject_CanAdminAProjectForNowItShouldBeAllOfThem()
    {
        var proj = new Project("Pepe", "pepito comio una bomba");
        _database.Projects.Add(proj);

        var result = _userService.GetAllUserWhichCanAdminNewProject();

        Assert.AreEqual(1, result.Length);
    }

    [TestMethod]
    public void GetAllUserWhichCanAdminNewProject_SystemManagerCantAdminANewProjectHeAlreadyIs()
    {
        var proj = new Project("Pepe", "pepito comio una bomba");
        
        _database.Projects.Add(proj);
        proj = _database.Projects.FindBy(p => p.Name.Equals("Pepe"));
        _systemManager = _userService.GetUserDto(_systemManager!.Email);

        var projectUser = new ProjectUser
        {
            ProjectId = proj!.Id,
            UserId = _systemManager!.Id,
            Role = Roles.ProjectManager,
        };
        
        _database.ProjectUsers.Add(projectUser);

        var result = _userService.GetAllUserWhichCanAdminNewProject();

        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void SetRandomPasswordWhitCorrectlyUsers_ReturnsNull()
    {

        var (_, error) = _userService.SetRandomPassword(_systemManager!, _userDto);

        Assert.IsNull(error);
    }

    [TestMethod]
    public void SetRandomPasswordUserApplicantIsSystemManager_ReturnsPassword()
    {
        var (newPass, _) = _userService.SetRandomPassword(_systemManager!, _userDto);

        Assert.IsFalse(string.Compare(newPass, "", StringComparison.Ordinal) == 0);
    }

    [TestMethod]
    public void SetRandomPasswordNonTrackedUserTarget_ReturnsError()
    {
        _userDto.Email = "fiumba@fiumb.com";

        if (_systemManager == null) return;
        var error = _userService.SetRandomPassword(_systemManager, _userDto).Item2;

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetRandomPasswordNonTrackedUserApplicant_ReturnsError()
    {
        if (_systemManager == null) return;
        _systemManager.Email = "fiumba@fiumb.com";

        var error = _userService.SetRandomPassword(_systemManager, _userDto).Item2;

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetRandomPasswordWithNonUserApplicantRoleSystemManager_ReturnsError()
    {
        var error = _userService.SetRandomPassword(_userDto, _userDto).Item2;

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetDefaultPasswordWithNonTrackUserApplicant_ReturnsError()
    {
        _userDto.Email = "fiumba@fiumb.com";

        var (_, error) = _userService.SetDefaultPassword(_systemManager!, _userDto);

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetDefaultPasswordWithNonUserApplicantRoleSystemManager_ReturnsError()
    {
        var (_, error) = _userService.SetDefaultPassword(_userDto, _userDto);

        Assert.IsInstanceOfType<Error>(error);
    }


    [TestMethod]
    public void SetDefaultPasswordWithInvalidUser_ReturnsError()
    {
        _userDto.Email = "askdjfal@jkgsjdk";

        var (_, error) = _userService.SetDefaultPassword(_systemManager!, _userDto);

        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void SetDefaultPasswordWhitCorrectlyUsers_ReturnsNull()
    {
        var (_, error) = _userService.SetDefaultPassword(_systemManager!, _userDto);

        Assert.IsNull(error);
    }

    [TestMethod]
    public void EditUserDTONameWithCorrectName_AssignCorrectly()
    {
        _userService.EditUserDtoName(_userDto, "test");
        Assert.AreEqual("test", _userService.GetUserDto(_userDto.Email)?.Name);
    }

    [TestMethod]
    public void EditUserDTONameWithEmptyName_ReturnError()
    {
        var error = _userService.EditUserDtoName(_userDto, "");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void EditUserDTONameOfANotTrackedUser_ReturnError()
    {
        var externalDto = new UserDTO
        {
            Name = "Pancho",
            Email = "papas@gmail.com"
        };
        var error = _userService.EditUserDtoName(externalDto, "Va");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void EditUserDTOLastNameWithCorrectName_AssignCorrectly()
    {
        _userService.EditUserDtoLastName(_userDto, "test");
        Assert.AreEqual("test", _userService.GetUserDto(_userDto.Email)?.LastName);
    }

    [TestMethod]
    public void EditUserDTOLastNameWithEmptyName_ReturnError()
    {
        var error = _userService.EditUserDtoLastName(_userDto, "");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void EditUserDTOLastNameOfANotTrackedUser_ReturnError()
    {
        var externalDto = new UserDTO
        {
            Name = "Pancho",
            Email = "papas@gmail.com"
        };
        var error = _userService.EditUserDtoLastName(externalDto, "Va");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void EditUserDTOBirthdateWithCorrectName_AssignCorrectly()
    {

        _userService.EditUserDtoBirthdate(_userDto, "10/10/2006");
        Assert.AreEqual("10/10/2006", _userService.GetUserDto(_userDto.Email)?.BirthDate);
    }

    [TestMethod]
    public void EditUserDTOBirthdateWithEmptyName_ReturnError()
    {
        var error = _userService.EditUserDtoBirthdate(_userDto, "");
        Assert.IsInstanceOfType<Error>(error);
    }

    [TestMethod]
    public void EditUserDTOBirthdateOfANotTrackedUser_ReturnError()
    {
        var externalDto = new UserDTO
        {
            Name = "Pancho",
            Email = "papas@gmail.com"
        };
        var error = _userService.EditUserDtoBirthdate(externalDto, "10/10/2010");
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void EditUserDTOBirthdateWithUnderAge_ReturnError()
    {
        Assert.IsInstanceOfType<Error?>(_userService.EditUserDtoBirthdate(_userDto, DateTime.Today.ToString("MM/dd/yyyy")));
    }

    [TestMethod]
    public void ExistsUserWithTrackedUser_ReturnTrue()
    {
        var exists = _userService.ExistUser(_userDto);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public void ExistsUserWithNotTrackedUser_ReturnFalse()
    {
        var externalDto = new UserDTO
        {
            Name = "Panchin",
            Email = "pantufla@mail.com",
            Password = "ContrataPass@1",
            BirthDate = "10/10/2000"
        };

        var exists = _userService.ExistUser(externalDto);

        Assert.IsFalse(exists);
    }
    
    [TestMethod]
    public void GetUserById_ReturnUser()
    {
        var (user, _) = _userService.GetUserById(1);
        Assert.IsNotNull(user);
    }
    
    [TestMethod]
    public void GetUserByIdWithNonTrackUser_ReturnError()
    {
        var (_, error) = _userService.GetUserById(12);
        Assert.IsInstanceOfType<Error>(error);
    }
    
    [TestMethod]
    public void GetInstanceNewContext_ReturnNewEmptyList()
    {
        ISqlContextFactory _contextFactoryTest = new InMemorySqlContextFactory("juason");
        var resourceService = UserService.GetInstance(_contextFactoryTest);
         
        Assert.IsNotNull(resourceService);
    }
}