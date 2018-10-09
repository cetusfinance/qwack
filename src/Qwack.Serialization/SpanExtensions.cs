using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qwack.Serialization
{
    internal static class SpanExtensions
    {
        public static void Write(ref this Span<byte> span, ushort value)
        {
            if (span.Length < sizeof(ushort)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(ushort));
        }
    }
}
