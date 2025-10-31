using System.Runtime.CompilerServices;

namespace NextId;

internal static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReverseString(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        int len = value.Length;

        Span<char> buffer = len <= 32 ? stackalloc char[len] : new char[len];

        for (int i = 0, j = len - 1; i < len; i++, j--)
        {
            buffer[i] = value[j];
        }

        return new string(buffer);
    }
}