using System.Globalization;
using Auxiliaries;
using Domain;

namespace Services.DTOs;

public class UserDTO
{
    public string Name;
    public string LastName;
    public string Email;
    public string BirthDate;
    public Roles Role;
    public string Password;
    private int _id;

    public (User?, Error?) FromEntity(byte[] password)
    {
        try
        {
            var user = new User(Name, LastName, Email, DateTime.Parse(BirthDate, CultureInfo.CreateSpecificCulture("en-US")), password);
            user.Role = Role;
            user.Id = this.Id;
            return (user, null);
        }
        catch (Exception e)
        {
            return (null, new Error("ArgumentError", e.Message));
        }
    }

    public static UserDTO ToEntity(User user)
    {
        var userDto = new UserDTO
        {
            Name = user.Name,
            LastName = user.LastName,
            BirthDate = user.BirthDate.ToString("MM/dd/yyyy"),
            Role = user.Role,
            Email = user.Email,
            Password = "",
            _id = user.Id
        };
        return userDto;
    }

    public int Id
    {
        get => _id;
    }
}
