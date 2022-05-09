using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Qwack.Serialization
{
    internal static class SpanExtensions
    {
        public static int ReadInt(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(int));
            return value;
        }

        public static byte ReadByte(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<byte>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(byte));
            return value;
        }

        public static short ReadShort(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<short>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(short));
            return value;
        }

        public static float ReadFloat(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<float>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(float));
            return value;
        }

        public static ushort ReadUShort(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(ushort));
            return value;
        }

        public static bool ReadBool(ref this Span<byte> span) => span.ReadByte() == 1;

        public static uint ReadUInt(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(uint));
            return value;
        }

        public static double ReadDouble(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<double>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(double));
            return value;
        }

        public static ulong ReadULong(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(ulong));
            return value;
        }

        public static DateTime ReadDateTime(ref this Span<byte> span) => new(span.ReadLong());

        public static long ReadLong(ref this Span<byte> span)
        {
            var value = Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(sizeof(long));
            return value;
        }

        public static string ReadString(ref this Span<byte> span)
        {
            var length = span.ReadInt();
            if (length == -1) return null;
            var newSpan = MemoryMarshal.Cast<byte, char>(span);
            var returnValue = newSpan.Slice(0, length).ToString();
            span = MemoryMarshal.Cast<char, byte>(newSpan.Slice(length));
            return returnValue;
        }

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

        public static void WriteString(ref this Span<byte> span, string value)
        {
            if (value == null)
            {
                span.WriteInt(-1);
            }
            else
            {
                span.WriteInt(value.Length);
                var chars = MemoryMarshal.AsBytes(value.AsSpan());
                chars.CopyTo(span);
                span = span.Slice(chars.Length);
            }
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
