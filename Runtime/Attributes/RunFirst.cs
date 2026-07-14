using System;

namespace AetherNexus.FoundationPlatform.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RunFirst : Attribute
    {
        public RunFirst()
        {
            /* noop */
        }
    }
}