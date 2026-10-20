namespace Auxiliaries;

public class Error
{

    public enum Types
    {
        DefaultError,
        FormatError,
        OutOfRangeException,
        PermissionDenied,
        KeyAlreadyExist,
        KeyDoesNotExist,
        ArgumentError,
        AlreadyLogged,
        PasswordMissmatch,
        StateCannotBeChanged,
        DatabaseUpdateError,
        DatabaseRemoveError,
    }

    private Types _type;
    private string? Message { set; get; }


    public Error(string type, string message)
    {
        SetType(type);
        SetMessage(message);
    }

    public Error(Types type, string message)
    {
        SetType(type);
        SetMessage(message);
    }

    public Error() => SetType("DefaultError");

    public void SetType(Types type)
    {
        _type = type;
    }

    public void SetType(string type)
    {
        string aux = type.ToLower();
        try
        {
            _type = Enum.Parse<Types>(aux, true);
        }
        catch
        {
            throw new ArgumentException($"Invalid type: {type}");
        }
    }

    public new string GetType() => _type.ToString();

    public string GetMessage() => Message ?? "";

    public void SetMessage(string message) => Message = message;
}
