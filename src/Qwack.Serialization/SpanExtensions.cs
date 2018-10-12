using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qwack.Serialization
{
    internal static class SpanExtensions
    {
        public static void WriteDateTime(ref this Span<byte> span, DateTime value)
        {
            if (span.Length < sizeof(long)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value.Ticks);
            span = span.Slice(sizeof(long));
        }

        public static void WriteBool(ref this Span<byte> span, bool value)
        {
            if (span.Length < sizeof(byte)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value ? 1 : 0);
            span = span.Slice(sizeof(byte));
        }

        public static void WriteByte(ref this Span<byte> span, byte value)
        {
            if (span.Length < sizeof(byte)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(byte));
        }

        public static void WriteLong(ref this Span<byte> span, long value)
        {
            if (span.Length < sizeof(long)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(long));
        }

        public static void WriteDouble(ref this Span<byte> span, double value)
        {
            if (span.Length < sizeof(double)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(double));
        }

        public static void WriteFloat(ref this Span<byte> span, float value)
        {
            if (span.Length < sizeof(float)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(float));
        }

        public static void WriteULong(ref this Span<byte> span, ulong value)
        {
            if (span.Length < sizeof(ulong)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(ulong));
        }

        public static void WriteUShort(ref this Span<byte> span, ushort value)
        {
            if (span.Length < sizeof(ushort)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(ushort));
        }

        public static void WriteInt(ref this Span<byte> span, int value)
        {
            if (span.Length < sizeof(int)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(int));
        }

        public static void WriteShort(ref this Span<byte> span, short value)
        {
            if (span.Length < sizeof(short)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(short));
        }

        public static void WriteUInt(ref this Span<byte> span, uint value)
        {
            if (span.Length < sizeof(uint)) throw new IndexOutOfRangeException();
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            span = span.Slice(sizeof(uint));
        }
    }
}
