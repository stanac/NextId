using System.Runtime.CompilerServices;

namespace NextId;

internal static class InternalConverters
{
    private const string Alphabet = "23456789BCDFGHJKLMNPQRSTVWXYZabcdfghjkmnpqrstvwxyz";
    private const int Base = 50;
    private const int FixedAlphabetLength = 12;
    private const int FixedNumberAlphabetLength = 20;
    public const int Max3Digits = 116993;
    private static readonly sbyte[] CharToValueMap = CreateCharToValueMap();

    public static string EncodeChecksum(int value)
    {
        if (value < 0 || value >= Max3Digits)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Expected value to be >= 0 and < {Max3Digits}");
        }

        Span<char> buffer = stackalloc char[3];
        int i = 3;

        do
        {
            int rem = value % Base;
            value /= Base;
            buffer[--i] = Alphabet[rem];
        } while (value > 0);

        // Left pad with zero symbol ('2')
        while (i > 0)
            buffer[--i] = Alphabet[0];

        return new string(buffer);
    }

    public static string EncodeToString(ulong value)
    {
        Span<char> buffer = stackalloc char[FixedAlphabetLength];
        int i = FixedAlphabetLength;

        do
        {
            ulong rem = value % Base;
            value /= Base;
            buffer[--i] = Alphabet[(int)rem];
        } while (value > 0);

        // Left pad with zero symbol ('2')
        while (i > 0)
            buffer[--i] = Alphabet[0];

        return new string(buffer);
    }

    public static string EncodeToNumberString(ulong value)
    {
        byte[] data = BitConverter.GetBytes(value);
        uint int1 = BitConverter.ToUInt32(data, 0);
        uint int2 = BitConverter.ToUInt32(data, 4);

        return int1.ToString().PadLeft(10, '0') + int2.ToString().PadLeft(10, '0');
    }

    public static ulong Decode(ReadOnlySpan<char> text)
    {
        if (text.Length != FixedAlphabetLength && text.Length != FixedNumberAlphabetLength && text.Length != 3)
            throw new ArgumentException($"String length expected to be {FixedAlphabetLength} or {FixedNumberAlphabetLength}, got: {text.Length}.");
        
        if (text.Length == FixedNumberAlphabetLength)
        {
            ReadOnlySpan<char> s1 = text.Slice(0, 10);
            ReadOnlySpan<char> s2 = text.Slice(10);

            uint i1 = uint.Parse(s1);
            uint i2 = uint.Parse(s2);

            byte[] bytes1 = BitConverter.GetBytes(i1);
            byte[] bytes2 = BitConverter.GetBytes(i2);

            byte[] all = [..bytes1, ..bytes2];

            return BitConverter.ToUInt64(all);
        }

        ulong value = 0;
        foreach (char c in text)
        {
            sbyte digit = (c < CharToValueMap.Length) ? CharToValueMap[c] : (sbyte)-1;
            if (digit == -1)
            {
                throw new FormatException($"Invalid character '{c}' in input.");
            }

            checked
            {
                value = value * Base + (ulong)digit;
            }
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Encode(Span<char> buffer, ulong value)
    {
        int i = buffer.Length;

        do
        {
            ulong rem = value % Base;
            value /= Base;
            buffer[--i] = Alphabet[(int)rem];
        } while (value > 0);

        while (i > 0)
            buffer[--i] = Alphabet[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EncodeChecksum(Span<char> buffer, int value)
    {
        int i = buffer.Length;

        do
        {
            int rem = value % Base;
            value /= Base;
            buffer[--i] = Alphabet[rem];
        } while (value > 0);

        while (i > 0)
            buffer[--i] = Alphabet[0];
    }

    private static sbyte[] CreateCharToValueMap()
    {
        var map = new sbyte[123];
        Array.Fill(map, (sbyte)-1);

        for (int i = 0; i < Alphabet.Length; i++)
        {
            map[Alphabet[i]] = (sbyte)i;
        }
        return map;
    }
}
