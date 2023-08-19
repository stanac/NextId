using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace NextId;

/// <summary>
/// Abstract class for strongly-typed, K-Sortable Id with Checksum in format: {prefix}-{TimeComponent}{RandomComponent}{Checksum}.
/// TimeComponent is 10 characters, RandomComponent is 11 characters and Checksum is 2 characters.
/// Prefix is user defined. TimeComponent is current time for new values (if not specified otherwise).
/// </summary>
#if DEBUG
[DebuggerDisplay("{DebugValue}")]
#endif
public abstract class Identifier<TSelf> : IEquatable<TSelf>
    where TSelf : Identifier<TSelf>, IParsable<TSelf>
{
    // ReSharper disable StaticMemberInGenericType
    private static readonly ThreadSafeRandom _rand = new();
    private static readonly DateTimeOffset _minTime = new(new DateTime(1995, 1, 1));
    private const int ChecksumLength = 3;

#if DEBUG
    internal string DebugValue { get; }
#endif

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
    /// Time component of the id
    /// </summary>
    public DateTimeOffset TimeComponent { get; }

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
        TimeComponent = time;

#if DEBUG
        DebugValue = $"{GetType().Name}: {Value}";
#endif
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
        EnsureValid(Value, Prefix, Salt);
        // ReSharper restore VirtualMemberCallInConstructor

        if (IsTimeComponentValid(Value, out DateTimeOffset? td))
        {
            TimeComponent = td.Value;
        }
        else
        {
            // EnsureValid should throw if time component is not valid
            throw new UnreachableException();
        }

#if DEBUG
        DebugValue = $"{GetType().Name}: {Value}";
#endif
    }

    #region Implementation methods

    private string Generate(DateTimeOffset time)
    {
        if (time < _minTime)
        {
            throw new ArgumentException("Time cannot be before year 1995");
        }

        // {prefix}-{TimeComponent}{RandomComponent}{Checksum}

        ValidateSaltAndPrefix(Salt, Prefix);

        string timeValue = Base50.ToString(time.ToUnixTimeMilliseconds()) + Base50.ToString(time.Microsecond, 2);
        string random = Base50.ToString(_rand.NextInt64(), 11);

        if (random.Length > 11) random = random.Substring(1, 11);
        
        string value = $"{Prefix}-{timeValue}{random}";

        string checksum = Hash(value, Salt);

        return $"{value}{checksum}";
    }

    protected static bool IsValid(string value, string prefix, string salt)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Length > 40) return false;

        if (value.Count(c => c == '-') != 1) return false;

        if (!value.StartsWith(prefix + "-")) return false;

        string checksum = value.Substring(value.Length - ChecksumLength);
        string valueWithoutChecksum = value.Substring(0, value.Length - ChecksumLength);

        string secondPart = value.Split('-')[1];

        if (secondPart.Length != 24)
        {
            return false;
        }

        if (checksum != Hash(valueWithoutChecksum, salt)) return false;
        
        if (!IsTimeComponentValid(value, out _))
        {
            return false;
        }

        return true;
    }

    private static void EnsureValid(string value, string prefix, string salt)
    {
        if (!IsValid(value, prefix, salt))
        {
            throw new FormatException("Wrong format for identifier");
        }
    }

    private static bool IsTimeComponentValid(string value, [NotNullWhen(true)]out DateTimeOffset? timeComponent)
    {
        string timePart = value.Split('-')[1].Substring(10);
        string milliseconds = timePart.Substring(0, 8);
        string microseconds = timePart.Substring(8);

        DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds(Base50.ToLong(milliseconds));
        timeComponent = dt.AddMicroseconds(Base50.ToLong(microseconds));

        if (timeComponent < _minTime)
        {
            timeComponent = null;
            return false;
        }

        return true;
    }

    private static string Hash(string input, string salt)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input + salt);
        byte[] hash = SHA256.HashData(inputBytes);
        long hashValue = Math.Abs(BitConverter.ToInt64(hash));
        string hashString = Base50.ToString(hashValue);

        return hashString.Substring(hashString.Length - ChecksumLength);
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