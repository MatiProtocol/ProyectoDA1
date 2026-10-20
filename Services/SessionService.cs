using Auxiliaries;
using DataAccess;
using DBContext;
using Domain;
using Services.DTOs;

namespace Services;

public class SessionService
{

    private static User? _current;
    private ISqlContextFactory _contextFactory;
    private PureDatabase _database;
    private static SessionService? _instance = null;

    public static SessionService GetInstance(ISqlContextFactory? contextFactory = null)
    {
        if (_instance == null) _instance = new SessionService(contextFactory);
        return _instance;
    }
    
    public static SessionService GetNewInstance(ISqlContextFactory? contextFactory = null)
    {
        var instance = new SessionService(contextFactory);
        return instance;
    }

    private SessionService(ISqlContextFactory? contextFactory = null)
    {
        if (contextFactory == null) contextFactory = new SqlContextFactory();
        _contextFactory = contextFactory;
        _database =PureDatabase.GetNewInstance(contextFactory);
    }

    public UserDTO? GetCurrent()
    {
        if (_current == null) return null;
        return UserDTO.ToEntity(_current);
    }

    public Error? Login(string email, string pass)
    {
        if (GetCurrent() != null) return new Error("AlreadyLogged", "Ya hay un usuario loggedo (Esto no deberia suceder, por ahora)");

        var userService =UserService.GetNewInstance(_contextFactory);
        var user = _database.Users.FindBy(u => u.Email.Equals(email));

        if (user == null) return new Error("ArgumentError", "Usuario y/o contraseña incorrecta");
        
        var userDto = UserDTO.ToEntity(user);
        var correct = userService.VerifyPassword(userDto, pass);
        if (!correct) return new Error("ArgumentError", "Usuario y/o contraseña incorrecta");

        _current = user;

        return null;
    }

    public void Logout() => _current = null;

}
