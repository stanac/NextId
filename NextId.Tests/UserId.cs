namespace NextId.Tests;

public class UserId : Identifier<UserId>
{
    private const string PrefixConst = "user";
    private const string SaltConst = "99AAB45utg";

    protected override string Prefix => PrefixConst;

    // Salt for checksum hash
    protected override string Salt => SaltConst;

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

    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
    
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
