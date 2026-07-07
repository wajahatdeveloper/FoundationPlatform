using System;

namespace FoundationPlatform.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RunLast : Attribute
    {
        public RunLast()
        {
            /* noop */
        }
    }
}