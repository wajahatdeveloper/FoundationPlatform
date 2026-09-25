namespace AetherNexus.FoundationPlatform.Logging
{
    /// <summary>
    /// Truncates log messages from the bottom at newline boundaries for fixed-size buffers (e.g. FixedString4096Bytes).
    /// </summary>
    public static class LogMessageTruncation
    {
        public const int MaxFixedStringLength = 4096;

        /// <summary>
        /// Budget for a telemetry signal record, which carries a one-line summary rather than the
        /// full message plus stack trace that the file and console sinks receive.
        /// </summary>
        public const int MaxSignalLength = 512;

        private const string TruncatedSuffix = "\n... (truncated)";

        /// <summary>
        /// Truncates from the bottom at newline boundaries so length ≤ maxLength. Appends suffix when content was cut.
        /// </summary>
        public static string TruncateFromBottom(string value, int maxLength)
        {
            if (value == null || value.Length <= maxLength)
                return value;

            int suffixLen = TruncatedSuffix.Length;
            int effectiveLimit = maxLength - suffixLen;
            if (effectiveLimit <= 0)
                return value.Substring(0, maxLength);

            int lastNewline = value.LastIndexOf('\n', effectiveLimit, effectiveLimit + 1);
            int cutFrom;
            if (lastNewline >= 0)
            {
                cutFrom = lastNewline;
                if (lastNewline > 0 && value[lastNewline - 1] == '\r')
                    cutFrom = lastNewline - 1;
            }
            else
                cutFrom = effectiveLimit;

            return value.Substring(0, cutFrom) + TruncatedSuffix;
        }

        /// <summary>Truncates using MaxFixedStringLength.</summary>
        public static string TruncateFromBottom(string value) => TruncateFromBottom(value, MaxFixedStringLength);
    }
}
