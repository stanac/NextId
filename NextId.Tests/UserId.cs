namespace NextId.Tests;

public class UserId : Identifier<UserId>
{
    private const string PrefixConst = "user";
    private const string SaltConst = "99AAB45utg";
    protected override string Prefix => PrefixConst;
    protected override string Salt => SaltConst;

    public UserId() { }
    
    // constructor with time component
    public UserId(DateTimeOffset dt) : base(dt) { }

    // constructor for parsing
    private UserId(string value) : base(value) { }

    public static UserId NewId() => new();

    public static UserId Parse(string value) => new(value);

    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
    
    // you can add TryParse if needed
}
