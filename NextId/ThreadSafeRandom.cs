namespace NextId;

internal static class ThreadSafeRandom
{
    private static readonly object Sync = new();
    private static readonly Random Random = new();

    public static long NextInt64()
    {
        lock (Sync)
        {
            return Random.NextInt64();
        }
    }

    public static int Next(int max)
    {
        lock (Sync)
        {
            return Random.Next(max);
        }
    }
}