using System;
using System.Text;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RunAfter : Attribute
    {
        private Type[] _deps; /* dependencies */

        public Type[] All
        {
            get { return _deps; }
        }

        public RunAfter(params Type[] deps)
        {
            if (deps is null)
            {
                throw new ArgumentNullException(nameof(deps));
            }

            foreach (Type type in deps)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    throw new ArgumentException(
                        string.Format("The type '{0}' is not assignable from {1}.", type.Name, typeof(MonoBehaviour).Name),
                        nameof(deps));
                }
            }

            this._deps = deps;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < _deps.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                sb.Append(_deps[i].FullName);
            }

            return sb.Append("]").ToString();
        }
    }
}