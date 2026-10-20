using System.Net.Mail;
using System.Globalization;
using Auxiliaries;


namespace Domain;

public class User
{
    private int _id;
    
    private string _name;

    private string _lastName;

    private string _email;

    private DateTime _birthDate;

    private byte[] _password;

    private Roles _role;

    public User() {}

    public User(string name, string lastName, string email, DateTime birthDate, byte[] password)
    {
        Email = email;
        BirthDate = birthDate;
        _role = Roles.ProjectMember;
        _name = name;
        _lastName = lastName;
        _password = password;
    }

    public int Id
    {
        get => _id;
        set => _id = value;
    }
    
    public string Name
    {
        get => _name;
        set => _name = value;
    }


    public string LastName
    {
        get => _lastName;
        set => _lastName = value;
    }

    public string Email
    {
        get => _email;
        set
        {
            var error = ValidateEmailFormat(value);
            if (error != null) throw new ArgumentException(error.GetMessage());
            _email = value;
        }
    }

    public DateTime BirthDate
    {
        get => _birthDate;
        set
        {
            var rangeError = ValidateDateRange(value);
            if (rangeError != null) throw new ArgumentException(rangeError.GetMessage());
            _birthDate = value;
        }
    }

    public byte[] Password
    {
        get => _password;
        set => _password = value;
    }

    public Roles Role
    {
        get => _role;
        set => _role = value;
    }
    
    private static Error? ValidateDateRange(DateTime birthDate)
    {
        var today = DateTime.Today;

        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age)) age--;

        return age < 18 ? new Error("outOfRangeException", "User must be at least 18 years old") : null;
    }

    public static Error? ValidateEmailFormat(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return null;
        }
        catch
        {
            return new Error("formatError", "Invalid email format");
        }
    }



}
