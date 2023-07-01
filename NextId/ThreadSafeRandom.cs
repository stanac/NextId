namespace NextId;

internal class ThreadSafeRandom
{
    private static readonly object Sync = new();
    private readonly Random _random = new();

    public long NextInt64()
    {
        lock (Sync)
        {
            return _random.NextInt64();
        }
    }
}