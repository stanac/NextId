using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace NextId;

/// <summary>
/// Helper class to be able to easily switch hash algorithm
/// </summary>
internal static class HashHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] HashDataTo16Bytes(byte[] data) => XxHash128.Hash(data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] HashDataTo16Bytes(Span<byte> data) => XxHash128.Hash(data);
}

