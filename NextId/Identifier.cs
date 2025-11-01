using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        SetValueFromParsing(value);
    }

    private void SetValueFromParsing(string value)
    {
        if (value.Length < Prefix.Length + 40)
        {
            _value = value;
        }
        else
        {
            _numberValue = value;
        }
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

                ulong mask = _timeComponentMask ??= BitConverter.ToUInt64(HashHelper.HashDataTo16Bytes(Encoding.UTF8.GetBytes(salt)), 0);
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

    private static byte[] GetSaltHash(string salt) => HashHelper.HashDataTo16Bytes(Encoding.UTF8.GetBytes(salt));

    private static byte[] GetPrefixHash(string prefix) => HashHelper.HashDataTo16Bytes(Encoding.UTF8.GetBytes(prefix));

    private int ComputeChecksum() => ComputeChecksum(Prefix, Salt, TimeComponent, RandomComponent);

    private static int ComputeChecksum(string prefix, string salt, ulong time, ulong random)
    {
        // 16 (salt hash) + 16 (prefix hash) + 8 (time) + 8 (random) = 48 bytes.

        Span<byte> data = stackalloc byte[48];

        byte[] hashSalt = _saltHashBytes ??= HashHelper.HashDataTo16Bytes(Encoding.UTF8.GetBytes(salt));
        byte[] prefixHash = _prefixHashBytes ??= HashHelper.HashDataTo16Bytes(Encoding.UTF8.GetBytes(prefix));

        hashSalt.CopyTo(data.Slice(0, 16));

        prefixHash.CopyTo(data.Slice(16, 16));

        BitConverter.TryWriteBytes(data.Slice(32, 8), time);
        BitConverter.TryWriteBytes(data.Slice(40, 8), random);

        byte[] finalHash = HashHelper.HashDataTo16Bytes(data);

        Span<byte> hashSpan = finalHash.AsSpan();
        int part1 = BitConverter.ToInt32(hashSpan.Slice(0, 4));
        int part2 = BitConverter.ToInt32(hashSpan.Slice(4, 4));
        int part3 = BitConverter.ToInt32(hashSpan.Slice(8, 4));
        int part4 = BitConverter.ToInt32(hashSpan.Slice(12, 4));

        int combinedHash = part1 ^ part2 ^ part3 ^ part4;

        return Math.Abs(combinedHash) % InternalConverters.Max3Digits;
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
        const int timeLength = 12;
        const int randomLength = 12;
        const int checksumLength = 3;

        int totalLength = Prefix.Length + 1 + timeLength + randomLength + checksumLength;

        return string.Create(totalLength, this, (span, id) =>
        {
            int position = 0;

            id.Prefix.AsSpan().CopyTo(span);
            position += id.Prefix.Length;
            span[position++] = '-';

            InternalConverters.Encode(span.Slice(position, timeLength), id.TimeComponent);
            position += timeLength;

            InternalConverters.Encode(span.Slice(position, randomLength), id.RandomComponent);
            position += randomLength;

            InternalConverters.EncodeChecksum(span.Slice(position, checksumLength), id.Checksum);
        });
    }

    private string GetNumberValue()
    {
        int totalLength = Prefix.Length + 1 + 20 + 20 + 6;

        return string.Create(totalLength, this, (span, id) =>
        {
            int position = 0;

            id.Prefix.AsSpan().CopyTo(span);
            position += id.Prefix.Length;
            span[position++] = '-';

            ulong maskedTime = id.TimeComponent ^ id.TimeComponentMask;
            maskedTime.TryFormat(span.Slice(position, 20), out _, "D20");

            span.Slice(position, 20).Reverse();
            position += 20;

            byte[] data = BitConverter.GetBytes(id.RandomComponent);
            uint int1 = BitConverter.ToUInt32(data, 0);
            uint int2 = BitConverter.ToUInt32(data, 4);
            int1.TryFormat(span.Slice(position, 10), out _, "D10");
            position += 10;
            int2.TryFormat(span.Slice(position, 10), out _, "D10");
            position += 10;

            id.Checksum.TryFormat(span.Slice(position, 6), out _, "D6");
        });
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