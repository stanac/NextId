namespace NextId.Tests;

public class UserId : Identifier
{
    /// <summary>
    /// Identifier prefix
    /// </summary>
    protected override string Prefix => "user";

    /// <summary>
    /// Salt for checksum hash
    /// </summary>
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

    // you can add TryParse if you need it


}