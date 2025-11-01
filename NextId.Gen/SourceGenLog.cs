namespace NextId.Gen
{
    internal static class SourceGenLog
    {
#if DEBUG
        private const bool IsDebug = true;
#else
    private const bool IsDebug = false;
#endif

        internal static bool ForceEnable { get; set; } = true;

#pragma warning disable CS0162 // Unreachable code detected
        private static bool IsEnabled => IsDebug || ForceEnable;
#pragma warning restore CS0162 // Unreachable code detected

        internal static string LogDirPath => @"d:\temp\SourceGenLog\";

        public static void Log(string message)
        {
            if (!IsEnabled)
            {
                return;
            }

            string filePath = Path.Combine(LogDirPath, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd") + "_logs.txt");

            message = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} {message}\r\n";

#pragma warning disable RS1035
            File.AppendAllText(filePath, message);
#pragma warning restore RS1035

        }

    }
}
