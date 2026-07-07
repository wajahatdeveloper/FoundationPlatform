namespace FoundationPlatform.DebugX
{
    /// <summary>
    /// Caller information (Editor only typically)
    /// </summary>
    public readonly struct CallerInfo
    {
        public readonly string MemberName;
        public readonly string FilePath;
        public readonly int LineNumber;

        public CallerInfo(string memberName, string filePath, int lineNumber)
        {
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        public bool IsEmpty => string.IsNullOrEmpty(MemberName);
    }
}

