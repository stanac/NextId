namespace NextId.Tests;

public class UserId : Identifier<UserId>
{
    protected override string Prefix => "user";

    // Salt for checksum hash
    protected override string Salt => "99AAB45utg";

    // default constructor, generates new id
    public UserId()
    {
    }
    
    // constructor with time component
    public UserId(DateTimeOffset dt) : base(dt) { }

    // constructor for parsing
    private UserId(string value) : base(value)
    {
    }

    public static UserId NewId() => new();

    public static UserId Parse(string value) => new(value);
    
    public static bool TryParse(string value, out UserId? result)
    {
        try
        {
            result = Parse(value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
    
}
