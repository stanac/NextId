using System.Diagnostics.CodeAnalysis;

namespace NextId.Tests;

public class UserId : Identifier<UserId>, IParsable<UserId>
{
    private const string PrefixConst = "user";
    private const string SaltConst = "99AAB45utg";
    protected override string Prefix => PrefixConst;
    protected override string Salt => SaltConst;

    private UserId() { }
    
    // constructor with time component
    public UserId(DateTimeOffset dt) : base(dt) { }

    // constructor for parsing
    private UserId(string value) : base(value) { }

    public static UserId NewId() => new();

    public static UserId Parse(string s) => Parse(s, null);

    public static UserId Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)]out UserId? result)
    {
        try
        {
            result = Parse(s!, provider);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
}
