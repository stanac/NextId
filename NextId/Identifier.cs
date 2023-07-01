using System.Security.Cryptography;
using System.Text;

namespace NextId;

/// <summary>
/// Abstract class for strongly-typed, K-Sortable Id with checksum in format: {prefix}-{timeComponent}-{version}-{randomComponent}-{checksum}.
/// Where prefix is user defined, timeComponent is current time for new values (if not specified otherwise).
/// First and only version at the moment is 0.
/// </summary>
public abstract class Identifier<TSelf> : IEquatable<TSelf>
    where TSelf : Identifier<TSelf>
{
    private const string CurrentVersion = "0";
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ThreadSafeRandom _rand = new();
    
    /// <summary>
    /// Value to use as prefix for Id, only ASCII letters and digits allowed. Less than 12 characters in length.
    /// </summary>
    protected abstract string Prefix { get; }
    
    /// <summary>
    /// Value to use as salt for checksum hashing. Less than 33 characters in length.
    /// </summary>
    protected abstract string Salt { get; }

    /// <summary>
    /// Id value
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Constructor for generating new values
    /// </summary>
    protected Identifier()
        : this(DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Constructor for generating new values for specific time
    /// </summary>
    /// <param name="time">Time component of the id</param>
    protected Identifier(DateTimeOffset time)
    {
        Value = Generate(time);
    }

    /// <summary>
    /// Constructor for parsing existing values
    /// </summary>
    /// <param name="value">Id value to parse</param>
    protected Identifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value not set", nameof(value));

        Value = value;

        // ReSharper disable VirtualMemberCallInConstructor
        Validate(Value, Prefix, Salt);
        // ReSharper restore VirtualMemberCallInConstructor
    }

    #region Implementation methods
    
    private string Generate(DateTimeOffset time)
    {
        ValidateSaltAndPrefix(Salt, Prefix);

        string timeValue = Base55.ToString(time.ToUnixTimeMilliseconds());
        string random = Base55.ToString(_rand.NextInt64(), 11) + Base55.ToString(_rand.NextInt64(), 11);

        string value = $"{Prefix}-{timeValue}-{CurrentVersion}-{random}";

        string checksum = Hash(value + Salt);

        return $"{value}-{checksum}";
    }
    
    private static void Validate(string value, string prefix, string salt)
    {
        ValidateSaltAndPrefix(salt, prefix);

        if (value.Length > 100)
        {
            throw new ArgumentException("Value too large", nameof(value));
        }
        
        if (!value.StartsWith(prefix + "-")) throw new ArgumentException("Prefix doesn't match.", nameof(prefix));

        string[] parts = value.Split("-");

        if (parts.Length != 5 || parts[2] != CurrentVersion)
        {
            ThrowFormatException();
        }
        
        string valueWithoutChecksum = string.Join("-", parts.Take(4));
        string newChecksum = Hash(valueWithoutChecksum + salt);

        if (newChecksum != parts[4])
        {
            ThrowFormatException();
        }
    }

    private static void ThrowFormatException(string comment = "")
    {
        string error = "Format not valid: " + comment;
        error = error.TrimEnd().TrimEnd(':').TrimEnd('.') + ".";
        throw new FormatException(error);
    }

    private static string Hash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = SHA256.HashData(inputBytes);
        long hashValue = Math.Abs(BitConverter.ToInt64(hash));
        string hashString = Base55.ToString(hashValue);

        return hashString.Substring(hashString.Length - 3);
    }

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private static void ValidateSaltAndPrefix(string salt, string prefix)
    {
        if (salt.Length > 32) throw new InvalidOperationException("Salt too large. Limit is 32 characters");
        if (prefix.Length > 11) throw new InvalidOperationException("Prefix too large. Limit is 32 characters");

        if (prefix.Any(c => !char.IsAsciiLetterOrDigit(c)))
        {
            throw new InvalidOperationException("Prefix can contain only ASCII letters and digits.");
        }
    }

    #endregion Implementation methods

    #region Equals and overrides

    /// <summary>
    /// Are values equal
    /// </summary>
    /// <param name="other">other object</param>
    /// <returns>True if other object is of the same type and value</returns>
    public bool Equals(TSelf? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    /// <summary>
    /// Are values equal
    /// </summary>
    /// <param name="obj">other object</param>
    /// <returns>True if other object is of the same type and value</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TSelf)obj);
    }

    /// <summary>
    /// Returns hash code of Identifier
    /// </summary>
    /// <returns>Integer hash code</returns>
    public override int GetHashCode() => HashCode.Combine(Prefix, Value); // prefix first to differ this hash code from string hash code

    /// <summary>
    /// Are two identifiers equal
    /// </summary>
    /// <param name="id1">Identifier</param>
    /// <param name="id2">Identifier</param>
    /// <returns>True if identifiers are equal</returns>
    public static bool operator ==(Identifier<TSelf>? id1, Identifier<TSelf>? id2)
    {
        if (id1 is null && id2 is null) return true;
        if (id1 is null || id2 is null) return false;

        return id1.Value == id2.Value;
    }

    /// <summary>
    /// Are two identifiers different
    /// </summary>
    /// <param name="id1">Identifier</param>
    /// <param name="id2">Identifier</param>
    /// <returns>True if identifiers are different</returns>
    public static bool operator !=(Identifier<TSelf>? id1, Identifier<TSelf>? id2) => !(id1 == id2);

    /// <summary>
    /// Converts identifier to string value
    /// </summary>
    /// <returns>String value that can be parsed</returns>
    public override string ToString() => Value;

    #endregion Equals and overrides
}