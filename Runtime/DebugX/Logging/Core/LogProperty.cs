namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Property type for structured logging
    /// </summary>
    public enum PropertyType
    {
        Scalar,      // Simple value (string, int, etc.)
        Structured,  // Complex object (will be serialized)
        Destructured // Object to log with all properties
    }

    /// <summary>
    /// A key-value property for structured logging
    /// </summary>
    public readonly struct LogProperty
    {
        public readonly string Key;
        public readonly object Value;
        public readonly PropertyType Type;

        public LogProperty(string key, object value, PropertyType type)
        {
            Key = key;
            Value = value;
            Type = type;
        }

        public LogProperty(string key, object value) : this(key, value, PropertyType.Scalar) { }
    }
}

