// File: \NextId\NextId\Identifier.cs

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    private static bool _saltAndPrefixValidated;
    private static readonly ThreadSafeRandom Rand = new();
    private static readonly DateTimeOffset MinTime = new(new DateTime(1995, 1, 1));
    private const int ChecksumLength = 3;
    private const int PayloadLength = 24; // Time(10) + Random(11) + Checksum(3)
    private const int NumericPayloadLength = PayloadLength * 2; // Each Base50 char becomes 2 digits
    private static string? _numberValueMask;
    private static readonly object MaskSync = new();
    
    private string? _numberValue;

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
    /// Internal K-Sortable Id value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Obfuscated Id value represented as numbers, suitable for public use (e.g., in URLs). This value is NOT sortable.
    /// </summary>
    public string NumberValue => _numberValue ??= GetNumberValue();

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
    /// Constructor for parsing existing values (from Value, NumberValue, or ToString()).
    /// </summary>
    /// <param name="value">Id value to parse</param>
    protected Identifier(string value)
    {
        // This constructor must handle both regular Base50 strings and masked numeric strings.
        // It uses the instance-specific Salt to unmask if necessary.
        string[] parts = value.Split('-');
        if (parts.Length == 2 && parts[1].All(char.IsDigit) && parts[1].Length == NumericPayloadLength)
        {
            // ReSharper disable VirtualMemberCallInConstructor
            string mask = GetMask(Salt);
            // ReSharper restore VirtualMemberCallInConstructor
            string unmaskedNumericPayload = ApplyXorMask(parts[1], mask);
            string base50Payload = Base50.GetStringValue(unmaskedNumericPayload);
            value = parts[0] + "-" + base50Payload;
        }

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
        if (time < MinTime)
        {
            throw new ArgumentException("Time cannot be before year 1995");
        }

        if (!_saltAndPrefixValidated)
        {
            ValidateSaltAndPrefix(Salt, Prefix);
            _saltAndPrefixValidated = true;
        }

        string timeValue = Base50.ToString(time.ToUnixTimeMilliseconds()) + Base50.ToString(time.Microsecond, 2);
        string random = Base50.ToString(Rand.NextInt64(), 11);

        if (random.Length > 11) random = random.Substring(1, 11);

        string value = $"{Prefix}-{timeValue}{random}";
        string checksum = Hash(value, Salt);

        return $"{value}{checksum}";
    }

    public static bool IsValid(string value, string prefix, string salt)
    {
        try
        {
            value = ConvertNumberValueIfNeeded(value, salt);
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Length > 40) return false;
        if (value.Count(c => c == '-') != 1) return false;
        if (!value.StartsWith(prefix + "-")) return false;

        string checksum = value.Substring(value.Length - ChecksumLength);
        string valueWithoutChecksum = value.Substring(0, value.Length - ChecksumLength);

        string secondPart = value.Split('-')[1];

        if (secondPart.Length != PayloadLength)
        {
            return false;
        }

        if (checksum != Hash(valueWithoutChecksum, salt)) return false;

        return IsTimeComponentValid(value, out _);
    }

    private static void EnsureValid(string value, string prefix, string salt)
    {
        if (!IsValid(value, prefix, salt))
        {
            throw new FormatException("Wrong format for identifier");
        }
    }

    private static bool IsTimeComponentValid(string value, [NotNullWhen(true)] out DateTimeOffset? timeComponent)
    {
        string timePart = value.Split('-')[1].Substring(0, 10);
        string milliseconds = timePart.Substring(0, 8);
        string microseconds = timePart.Substring(8);

        DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds(Base50.ToLong(milliseconds));
        timeComponent = dt.AddMicroseconds(Base50.ToLong(microseconds));

        if (timeComponent < MinTime)
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

    private static void ValidateSaltAndPrefix(string salt, string prefix)
    {
        if (salt.Length > 32) throw new InvalidOperationException("Salt too large. Limit is 32 characters");
        if (prefix.Length > 11) throw new InvalidOperationException("Prefix too large. Limit is 11 characters");
        if (prefix.Length < 3) throw new InvalidOperationException("Prefix too short. Min length is 3");
        if (string.IsNullOrWhiteSpace(prefix)) throw new InvalidOperationException("Prefix not set");
        if (string.IsNullOrWhiteSpace(salt)) throw new InvalidOperationException("Salt not set");
        if (salt.Any(char.IsWhiteSpace)) throw new InvalidOperationException("No whitespace chars in Salt allowed.");
        if (prefix.Any(char.IsWhiteSpace)) throw new InvalidOperationException("No whitespace chars in Prefix allowed.");

        if (prefix.Any(c => !char.IsAsciiLetterOrDigit(c)))
        {
            throw new InvalidOperationException("Prefix can contain only ASCII letters and digits.");
        }
    }

    private string GetNumberValue()
    {
        string payload = Value.Split('-')[1];
        string plainNumericPayload = Base50.GetNumberValue(payload);

        string mask = GetMask(Salt);
        string maskedNumericPayload = ApplyXorMask(plainNumericPayload, mask);

        return Prefix + "-" + maskedNumericPayload;
    }

    private static string ConvertNumberValueIfNeeded(string value, string salt)
    {
        string[] parts = value.Split('-');
        if (parts.Length != 2) return value;

        string idValue = parts[1];

        if (idValue.All(char.IsDigit) && idValue.Length == NumericPayloadLength)
        {
            string mask = GetMask(salt);
            string unmaskedIdValue = ApplyXorMask(idValue, mask);
            string base50Value = Base50.GetStringValue(unmaskedIdValue);
            return parts[0] + "-" + base50Value;
        }

        return value;
    }

    #endregion

    #region Obfuscation Methods

    /// <summary>
    /// Generates a deterministic numeric mask from the given salt. The result is cached for performance.
    /// </summary>
    private static string GetMask(string salt)
    {
        if (_numberValueMask is not null) return _numberValueMask;

        lock (MaskSync)
        {
            if (_numberValueMask is not null) return _numberValueMask;

            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            byte[] hash = SHA256.HashData(saltBytes);

            var maskBuilder = new StringBuilder(NumericPayloadLength);
            int hashIndex = 0;

            while (maskBuilder.Length < NumericPayloadLength)
            {
                maskBuilder.Append(hash[hashIndex] % 10);
                hashIndex = (hashIndex + 1) % hash.Length;
            }

            _numberValueMask = maskBuilder.ToString();
            return _numberValueMask;
        }
    }

    /// <summary>
    /// Applies a reversible XOR operation between two strings of digits.
    /// </summary>
    private static string ApplyXorMask(string input, string mask)
    {
        var result = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            int digit = input[i] - '0';
            int maskDigit = mask[i] - '0';
            result.Append(digit ^ maskDigit);
        }
        return result.ToString();
    }

    #endregion

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
        if (obj.GetType() != GetType()) return false;
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
    /// Converts identifier to its internal, K-Sortable string value.
    /// </summary>
    /// <returns>String value that can be parsed</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Converts identifier to its obfuscated, numeric string value.
    /// </summary>
    public string ToNumberString() => NumberValue;

    #endregion
}