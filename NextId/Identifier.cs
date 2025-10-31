using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace NextId;

/// <summary>
/// Abstract class for strongly-typed, K-Sortable Id with Checksum in format: {prefix}-{TimeComponent}{RandomComponent}{Checksum}.
/// Prefix is user defined. TimeComponent is current time for new values (if not specified otherwise).
/// TimeComponent is obfuscated with XOR with <see cref="Salt"/> hash for <see cref="NumberValue"/>, it's not obfuscated for <see cref="Value"/>/
/// </summary>
[DebuggerDisplay("{DebugValue}")]
public abstract class Identifier<TSelf> : IEquatable<TSelf>
    where TSelf : Identifier<TSelf>, IParsable<TSelf>
{
    // ReSharper disable StaticMemberInGenericType
    private static bool _saltAndPrefixValidated;
    private static readonly DateTimeOffset MinTime = new(new DateTime(1975, 1, 1));
    private static byte[]? _saltHashBytes;
    private static byte[]? _prefixHashBytes;
    private static ulong? _timeComponentMask;
    private int? _checksum;
    
    private string? _numberValue;
    private string? _value;
    private string? _debugValue;

    internal string DebugValue => _debugValue ??= $"{GetType().Name}: {Value}";

    /// <summary>
    /// Value to use as prefix for Id, only ASCII letters and digits allowed. Less than 12 characters in length.
    /// </summary>
    protected abstract string Prefix { get; }

    /// <summary>
    /// Value to use as salt for checksum hashing. Less than 33 characters in length.
    /// </summary>
    protected abstract string Salt { get; }

    /// <summary>
    /// K-Sortable Id value.
    /// </summary>
    public string Value => _value ??= GetValue();

    /// <summary>
    /// Obfuscated Id value represented as numbers, suitable for public use (e.g., in URLs). This value is NOT sortable.
    /// </summary>
    public string NumberValue => _numberValue ??= GetNumberValue();

    /// <summary>
    /// Time component of the id
    /// </summary>
    public ulong TimeComponent { get; }

    /// <summary>
    /// Random component of the id
    /// </summary>
    protected ulong RandomComponent { get; }

    protected int Checksum => _checksum ??= ComputeChecksum();

    private ulong TimeComponentMask => _timeComponentMask ??= ComputeTimeComponentMask();

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
        if (time < MinTime)
        {
            throw new ArgumentException(nameof(time), $"Time cannot be less than {MinTime:u}");
        }

        TimeComponent = (ulong)time.ToUnixTimeMilliseconds() * 1000L + (ulong)time.Microsecond;
        RandomComponent = (ulong)ThreadSafeRandom.NextInt64();
        ValidateSaltAndPrefix();
    }

    /// <summary>
    /// Constructor for parsing existing values (from Value or NumberValue).
    /// </summary>
    /// <param name="value">Id value to parse</param>
    protected Identifier(string value)
    {
        ValidateSaltAndPrefix();

        if (!IsValid(value, out ulong timeComp, out ulong randomComp))
        {
            throw new FormatException($"Value `{value}` is not valid.");
        }

        TimeComponent = timeComp;
        RandomComponent = randomComp;
    }

    #region Implementation methods

    private bool IsValid(string value, out ulong timeComponent, out ulong randomComponent) => IsValid(value, Prefix, Salt, out timeComponent, out randomComponent);

    public static bool IsValid(string value, string prefix, string salt) => IsValid(value, prefix, salt, out _, out _);

    private static bool IsValid(string value, string prefix, string salt, out ulong timeComponent, out ulong randomComponent)
    {
        timeComponent = 0;
        randomComponent = 0;

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length < prefix.Length + 10 || value.Length > 100)
        {
            return false;
        }

        if (!value.StartsWith($"{prefix}-", StringComparison.Ordinal))
        {
            return false;
        }

        if (value.Count(c => c == '-') != 1)
        {
            return false;
        }

        ReadOnlySpan<char> idPart = value.AsSpan(prefix.Length + 1);

        bool isNumbersFormat = true;
        foreach (char c in idPart)
        {
            if (!char.IsAsciiDigit(c))
            {
                isNumbersFormat = false;
                break;
            }
        }

        if (isNumbersFormat && idPart.Length < 40)
        {
            isNumbersFormat = false;
        }

        try
        {
            if (isNumbersFormat)
            {
                // --- NUMBER FORMAT (time is obfuscated with xor) ---
                if (idPart.Length != 46)
                {
                    return false;
                }

                ReadOnlySpan<char> timeReversed = idPart.Slice(0, 20);
                ReadOnlySpan<char> randomDigits = idPart.Slice(20, 20);
                ReadOnlySpan<char> checksumDigits = idPart.Slice(40, 6);

                Span<char> timeTemp = stackalloc char[20];
                for (int i = 0; i < 20; i++)
                {
                    timeTemp[i] = timeReversed[19 - i];
                }

                if (!ulong.TryParse(timeTemp, out ulong maskedTime))
                {
                    return false;
                }

                ulong mask = BitConverter.ToUInt64(SHA256.HashData(Encoding.UTF8.GetBytes(salt)), 0);
                timeComponent = maskedTime ^ mask;

                randomComponent = InternalConverters.Decode(randomDigits);

                if (!int.TryParse(checksumDigits, out int checksum))
                {
                    return false;
                }

                int computed = ComputeChecksum(prefix, salt, timeComponent, randomComponent);
                return computed == checksum;
            }
            else
            {
                // --- BASE50 FORMAT ---
                // Expected 12 + 12 + 3 = 27 chars
                if (idPart.Length != 27)
                {
                    return false;
                }

                ReadOnlySpan<char> timeSpan = idPart.Slice(0, 12);
                ReadOnlySpan<char> randomSpan = idPart.Slice(12, 12);
                ReadOnlySpan<char> checksumSpan = idPart.Slice(24, 3);

                timeComponent = InternalConverters.Decode(timeSpan);
                randomComponent = InternalConverters.Decode(randomSpan);
                ulong checksumLong = InternalConverters.Decode(checksumSpan);

                int checksum;

                if (checksumLong < InternalConverters.Max3Digits)
                {
                    checksum = (int)checksumLong;
                }
                else
                {
                    return false;
                }

                int computed = ComputeChecksum(prefix, salt, timeComponent, randomComponent);
                return computed == checksum;
            }
        }
        catch
        {
            return false;
        }
    }


    private byte[] GetSaltHash() => _saltHashBytes ??= GetSaltHash(Salt);

    private static byte[] GetSaltHash(string salt) => SHA256.HashData(Encoding.UTF8.GetBytes(salt));

    private byte[] GetPrefixHash() => _prefixHashBytes ??= GetPrefixHash(Prefix);

    private static byte[] GetPrefixHash(string prefix) => SHA256.HashData(Encoding.UTF8.GetBytes(prefix));

    private int ComputeChecksum() => ComputeChecksum(Prefix, Salt, TimeComponent, RandomComponent);
    
    private static int ComputeChecksum(string prefix, string salt, ulong time, ulong random)
    {
        byte[] hashSalt = SHA256.HashData(Encoding.UTF8.GetBytes(salt));
        byte[] prefixHash = SHA256.HashData(Encoding.UTF8.GetBytes(prefix));
        byte[] timeBytes = BitConverter.GetBytes(time);
        byte[] randomBytes = BitConverter.GetBytes(random);

        byte[] toCompute = new byte[96];
        hashSalt.CopyTo(toCompute, 0);
        prefixHash.CopyTo(toCompute, 32);
        timeBytes.CopyTo(toCompute, 64);
        randomBytes.CopyTo(toCompute, 80);

        byte[] hash = SHA256.HashData(toCompute);
        return Math.Abs(BitConverter.ToInt32(hash)) % InternalConverters.Max3Digits;
    }

    private ulong ComputeTimeComponentMask() => BitConverter.ToUInt64(GetSaltHash(), 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateSaltAndPrefix() => ValidateSaltAndPrefix(Salt, Prefix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateSaltAndPrefix(string salt, string prefix)
    {
        if (_saltAndPrefixValidated)
        {
            return;
        }

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

        _saltAndPrefixValidated = true;
    }

    private string GetValue()
    {
        return $"{Prefix}-{InternalConverters.EncodeToString(TimeComponent)}{InternalConverters.EncodeToString(RandomComponent)}{InternalConverters.EncodeChecksum(Checksum)}";
    }

    private string GetNumberValue()
    {
        return $"{Prefix}-{(TimeComponent ^ TimeComponentMask).ToString().PadLeft(20, '0').ReverseString()}{InternalConverters.EncodeToNumberString(RandomComponent)}{Checksum.ToString().PadLeft(6, '0')}";
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