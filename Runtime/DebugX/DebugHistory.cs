using System;
using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.Logging
{
    /// <summary>
    /// Fixed-capacity ring of timestamped debug entries. Push in time order; the oldest entry is overwritten
    /// when full. Reads copy into a caller-owned list, oldest first, so no per-read allocation or sort.
    /// </summary>
    public sealed class DebugHistory<T>
    {
        private readonly T[] _values;
        private readonly float[] _times;
        private int _start;
        private int _count;

        public DebugHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    $"{nameof(DebugHistory<T>)}<{typeof(T).Name}> requires a positive capacity.");
            _values = new T[capacity];
            _times = new float[capacity];
        }

        public int Count => _count;

        public int Capacity => _values.Length;

        public void Push(T value, float time)
        {
            int index;
            if (_count < _values.Length)
            {
                index = (_start + _count) % _values.Length;
                _count++;
            }
            else
            {
                index = _start;
                _start = (_start + 1) % _values.Length;
            }

            _values[index] = value;
            _times[index] = time;
        }

        public void Clear()
        {
            Array.Clear(_values, 0, _values.Length);
            _start = 0;
            _count = 0;
        }

        /// <summary>Clears <paramref name="into"/> and fills it with entries at or after <paramref name="cutoff"/>, oldest first.</summary>
        public void CopySince(float cutoff, List<T> into)
        {
            into.Clear();
            for (int i = 0; i < _count; i++)
            {
                int index = (_start + i) % _values.Length;
                if (_times[index] >= cutoff)
                    into.Add(_values[index]);
            }
        }

        /// <summary>Clears <paramref name="into"/> and fills it with every entry, oldest first.</summary>
        public void CopyAll(List<T> into)
        {
            into.Clear();
            for (int i = 0; i < _count; i++)
                into.Add(_values[(_start + i) % _values.Length]);
        }
    }
}
