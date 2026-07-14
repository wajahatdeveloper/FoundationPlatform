namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Typed log channel wrapper. Use LogChannels.* constants or implicit string conversion.
    /// </summary>
    public readonly struct LogChannel
    {
        public readonly string Name;

        public LogChannel(string name) => Name = name;

        public static implicit operator LogChannel(string name) => new LogChannel(name);

        public static implicit operator string(LogChannel channel) => channel.Name;

        public override string ToString() => Name;
    }
}
