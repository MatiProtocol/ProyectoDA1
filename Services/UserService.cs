using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DataAccess;
using Domain;
using Services.DTOs;
using Auxiliaries;
using DBContext;
using Microsoft.IdentityModel.Tokens;

namespace Services;


public class UserService
{
    private ISqlContextFactory _contextFactory;
    private PureDatabase _database = PureDatabase.GetInstance();
    private static UserService? _instance;

    public static UserService GetInstance(ISqlContextFactory? contextFactory = null)
    {
        if(_instance == null) _instance = new UserService(contextFactory);
        _instance._database =PureDatabase.GetNewInstance(contextFactory);
        return _instance;
    }
    
    public static UserService GetNewInstance(ISqlContextFactory? contextFactory = null)
    {
        var instance = new UserService(contextFactory);
        instance._database = PureDatabase.GetNewInstance(contextFactory);
        return instance;
    }

    private UserService(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        _contextFactory = contextFactory;
    }

    private (User?, Error?) HasPermissionToEdit(UserDTO applicant)
    {
        var user = _database.Users.FindBy(u => u.Email == applicant.Email);
        if (user == null) return (null, new Error(Error.Types.PermissionDenied, "Permisos Insuficientes`"));
        if (user.Role != Roles.SystemManager)
            return (user, new Error("ArgumentError", "Applicant must be a system manager"));

        return (user, null);
    }

    public Error? CreateUser(UserDTO applicant, UserDTO newUser)
    {
        var (_, error) = HasPermissionToEdit(applicant);
        if (error != null) return error;

        if (ValidatePassword(newUser.Password) != null) return new Error("ArgumentError", "Invalid password format");
        (var user, error) = newUser.FromEntity(EncryptPassword(newUser.Password));

        if (user == null) return error;

        return _database.Users.Add(user) != null ? new Error(Error.Types.KeyAlreadyExist, "Mail no esta disponible") : null;
    }

    public bool DeleteUser(UserDTO applicant, UserDTO user)
    {
        var (_, error) = HasPermissionToEdit(applicant);
        if (error != null) return false;

        (_, error) = HasPermissionToEdit(user);
        if (error == null) return false;

        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var projectThatLead = projectService.GetAllProjectThatUserAreLeader(user);
        var projectThatAreMember = projectService.GetAllProjectThatUserAreMember(user);
        var projectThatManage = projectService.GetAllProjectThatUserAreManager(user);

        if (!projectThatLead.IsNullOrEmpty() || !projectThatAreMember.IsNullOrEmpty() ||
            !projectThatManage.IsNullOrEmpty()) return false;

        var userToRemove = _database.Users.FindBy(u => u.Email == user.Email);
        return userToRemove != null && _database.Users.Remove(userToRemove.Id);
    }

    public UserDTO? GetUserDto(string email)
    {
        var user = GetUser(email).ToTuple().Item1;
        if (user == null) return null;
        return UserDTO.ToEntity(user);
    }

    public (User?, Error?) GetUser(string email)
    {
        var user = _database.Users.FindBy(u => u.Email == email);
        return user != null ? (user, null) : (null, new Error("ArgumentError", "User not exist"));
    }
    
    public (User?, Error?) GetUserById(int id)
    {
        var user = _database.Users.FindBy(u => u.Id == id);
        return user != null ? (user, null) : (null, new Error("ArgumentError", "User not exist"));
    }

    public Error? ChangeEmail(string oldEmail, string newEmail)
    {
        var emailError = User.ValidateEmailFormat(oldEmail) ?? User.ValidateEmailFormat(newEmail);
        if (emailError != null) return emailError;


        var user = _database.Users.FindBy(u => u.Email == oldEmail);
        if (user == null)
            return new Error(Error.Types.KeyDoesNotExist, "Usuario aplicante no existe");

        
        var userNewEmail = _database.Users.FindBy(u => u.Email == newEmail);
        if (userNewEmail != null )
            return new Error(Error.Types.KeyAlreadyExist, "Mail no esta disponible");
        
        user.Email = newEmail;
        return _database.Users.Update(user);
    }

    public bool VerifyPassword(UserDTO applicant, string password)
    {
        var (user, error) = GetUser(applicant.Email);
        return error == null && ComparePassword(user.Password, EncryptPassword(password));
    }

    public bool ChangePassword(UserDTO applicant, string applicantPass, string newPassword)
    {
        if (!VerifyPassword(applicant, applicantPass)) return false;
        if (ValidatePassword(newPassword) != null) return false;
        var errorTuple = GetUser(applicant.Email);
        var user = errorTuple.Item1;
        
        if(user == null) return false;
        user.Password = EncryptPassword(newPassword);

        return _database.Users.Update(user) == null;
    }

    private static Error? ValidatePassword(string password) =>
        PasswordFormatValidator(password) ? null : new Error("FormatError", "Invalid password format");

    private static bool PasswordFormatValidator(string password)
    {
        return HasUpper(password)
               && HasLower(password)
               && HasNumber(password)
               && HasSpecialChar(password)
               && HasValidLenght(password);
    }

    private static bool HasUpper(string str) => !str.Equals(str.ToLower());
    private static bool HasLower(string str) => !str.Equals(str.ToUpper());
    private static bool HasNumber(string str) => str.Any(char.IsNumber);
    private static bool HasSpecialChar(string str) => !str.All(char.IsLetterOrDigit);
    private static bool HasValidLenght(string str) => str.Length >= 8;

    public Error? ChangeRole(UserDTO applicant, string email, Roles newRole)
    {
        var (_, error) = HasPermissionToEdit(applicant);
        if (error != null) return error;

        (var user, error) = GetUser(email);
        if (error != null) return error;

        if (user!.Role == Roles.SystemManager) return new Error("ArgumentError", "No se puede cambiar el rol de un administrador");
        
        var userDto = UserDTO.ToEntity(user);
        var projectService = ProjectService.GetNewInstance(_contextFactory);

        var projectThatManage = projectService.GetAllProjectThatUserAreManager(userDto);
        var projectThatLeader = projectService.GetAllProjectThatUserAreLeader(userDto);

        if (user.Role > newRole)
        {
            if (user.Role >= Roles.ProjectLeader && !projectThatLeader.IsNullOrEmpty())
                return new Error("ArgumentError", "El usuario lidera algún proyecto");

            if (user.Role >= Roles.ProjectManager && !projectThatManage.IsNullOrEmpty())
                return new Error("ArgumentError", "El usuario es manager de algún proyectos");
        }

        user.Role = newRole;
        return _database.Users.Update(user);
    }

    public static byte[] EncryptPassword(string password)
    {
        var hash = SHA256.Create();
        var passBytes = Encoding.UTF8.GetBytes(password);
        return hash.ComputeHash(passBytes);
    }

    public UserDTO[] GetAllUserWhichCanAdminNewProject()
    {
        var usersAlreadyManaging = _database.ProjectUsers.Query().AsEnumerable()
            .Where(pu => pu.Role == Roles.ProjectManager)
            .Select(pu => pu.UserId)
            .ToList();

        var eligibleUsers = _database.Users.Query().AsEnumerable()
            .Where(u => u.Role >= Roles.ProjectManager && !usersAlreadyManaging.Contains(u.Id));
            
        return UserArrayToDto(eligibleUsers.ToArray());
    }

    public UserDTO[] GetAllUserDto() => UserArrayToDto(GetAllUser());
    public User[] GetAllUser() => _database.Users.GetAll().ToArray();

    public UserDTO[] UserArrayToDto(User[] users) => users.Select(UserDTO.ToEntity).ToArray();

    public Error? EditUserDtoName(UserDTO userDto, string newName)
    {
        if (string.IsNullOrEmpty(newName)) return new Error("ArgumentError", "El nombre no puede ser vacío");

        var (user, error) = GetUser(userDto.Email);
        if (user == null) return error;

        user.Name = newName;
        
        _database.Users.Update(user);

        _database.Users.Update(user);
        
        return null;
    }

    public Error? EditUserDtoLastName(UserDTO userDto, string newLastName)
    {
        if (string.IsNullOrEmpty(newLastName))
            return new Error("ArgumentError", "El nuevo apellido no puede ser vacío");

        var (user, error) = GetUser(userDto.Email);
        if (user == null) return error;

        user.LastName = newLastName;

        _database.Users.Update(user);
        
        return null;
    }

    public Error? EditUserDtoBirthdate(UserDTO userDto, string newDate)
    {
        if (string.IsNullOrEmpty(newDate)) return new Error("ArgumentError", "El nuevo apellido no puede ser vacío");

        var (user, error) = GetUser(userDto.Email);
        if (user == null) return error;
        try
        {
            user.BirthDate = DateTime.Parse(newDate, CultureInfo.CreateSpecificCulture("en-US"));
        }
        catch (ArgumentException e)
        {
            return new Error("ArgumentError", e.Message);
        }

        _database.Users.Update(user);

        _database.Users.Update(user);
        
        return null;
    }

    public (string, Error?) SetRandomPassword(UserDTO applicant, UserDTO target)
    {
        var (_, error) = HasPermissionToEdit(applicant);
        if (error != null) return ("", error);

        (var user, error) = GetUser(target.Email);
        if (user == null) return ("", error);

        var newPassword = GenerateRandomPassword();
        user.Password = EncryptPassword(newPassword);

        _database.Users.Update(user);

        return (newPassword, null);
    }

    private static string GenerateRandomPassword()
    {
        const string lowers = "abcdefghijklmnopqrstuvwxyz";
        const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string specialChars = "!@#$%^&*()-_=+[]{}|;:',.<>?/`~";

        var random = new Random();
        var length = random.Next(8, 21);

        char[] firstRequiredChars =
        [
            lowers[random.Next(lowers.Length)],
            uppers[random.Next(uppers.Length)],
            numbers[random.Next(numbers.Length)],
            specialChars[random.Next(specialChars.Length)]
        ];
        var restantes = length - firstRequiredChars.Length;

        var odds = lowers + uppers + numbers + specialChars;

        var randoString = new StringBuilder();
        randoString.Append(firstRequiredChars);

        for (var i = 0; i < restantes; i++) randoString.Append(odds[random.Next(odds.Length)]);

        return new string(randoString.ToString().OrderBy(c => random.Next()).ToArray());
    }

    public (string, Error?) SetDefaultPassword(UserDTO applicant, UserDTO target)
    {
        var (_, error) = HasPermissionToEdit(applicant);
        if (error != null) return ("", error);

        (var targetUser, error) = GetUser(target.Email);
        if (targetUser == null) return ("", new Error(Error.Types.KeyDoesNotExist, "Usuario objetivo no existe"));

        var newPassword = "Pass" + target.Email + target.BirthDate;
        targetUser.Password = EncryptPassword(newPassword);
        
        var errorUpdate = _database.Users.Update(targetUser);
        return errorUpdate != null ? ("", errorUpdate) : (newPassword, null);
    }

    private bool ComparePassword(byte[] oldPassword, byte[] newPassword) => oldPassword.SequenceEqual(newPassword);

    public bool ExistUser(UserDTO userDto)
    {
        var user = _database.Users.FindBy(u => u.Email == userDto.Email);
        return user != null;
    }
}