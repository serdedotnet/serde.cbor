using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serde.Cbor;

internal sealed partial class CborWriter<TWriter> : ISerializer
    where TWriter : IByteWriter
{
    private TWriter _out;

    public CborWriter(TWriter output)
    {
        _out = output;
    }

    public int BytesWritten { get; private set; } = 0;

    /// <remarks>
    /// This method is less efficient than writing multiple bytes at once.
    /// </remarks>
    private void WriteByte(byte value)
    {
        _out.WriteByte(value);
    }

    public void WriteBool(bool b)
    {
        WriteByte((byte)(b ? 0xf5 : 0xf4));
    }

    public void WriteU8(byte b) => WriteU64(b);

    public void WriteChar(char c) => WriteString(c.ToString());

    ITypeSerializer ISerializer.WriteCollection(ISerdeInfo typeInfo, int? length)
    {
        if (length is null)
        {
            throw new InvalidOperationException(
                "Cannot serialize a collection with an unknown length."
            );
        }
        if (typeInfo.Kind == InfoKind.List)
        {
            WriteMajorTypeArgument((uint)length.Value, 0x80);
        }
        else if (typeInfo.Kind == InfoKind.Dictionary)
        {
            BeginMap((int)length);
        }
        else
        {
            throw new InvalidOperationException("Expected a collection, found: " + typeInfo.Kind);
        }
        return new SerCollection(this);
    }

    private void BeginMap(int fieldCount)
    {
        WriteMajorTypeArgument((uint)fieldCount, 0xa0);
    }

    public void WriteDecimal(decimal d)
    {
        throw new NotImplementedException();
    }

    public void WriteU128(UInt128 u128)
    {
        throw new NotImplementedException(
            "128-bit integers are not yet supported in CBOR serialization."
        );
    }

    public void WriteI128(Int128 i128)
    {
        throw new NotImplementedException(
            "128-bit integers are not yet supported in CBOR serialization."
        );
    }

    public void WriteF64(double d)
    {
        WriteByte(0xfb);
        WriteBigEndian(d);
    }

    public void WriteF32(float f)
    {
        WriteByte(0xfa);
        WriteBigEndian(f);
    }

    public void WriteI16(short i16) => WriteI64(i16);

    public void WriteI32(int i32) => WriteI64(i32);

    void ISerializer.WriteI64(long i64) => WriteI64(i64);

    private void WriteI64(long i64)
    {
        if (i64 >= 0)
        {
            WriteU64((ulong)i64);
        }
        else
        {
            // CBOR major type 1: negative value is encoded as -1 - n
            ulong n = (ulong)(-1 - i64);
            WriteMajorTypeArgument(n, 0x20);
        }
    }

    public void WriteNull()
    {
        WriteByte(0xf6);
    }

    public void WriteDateTimeOffset(DateTimeOffset dt)
    {
        WriteTag(0);
        WriteString(FormatRfc3339(dt));
    }

    /// <summary>
    /// Formats a DateTimeOffset as RFC 3339 with minimal fractional seconds
    /// and 'Z' for UTC, matching CBOR tag 0 conventions.
    /// </summary>
    private static string FormatRfc3339(DateTimeOffset dt)
    {
        if (dt.Offset == TimeSpan.Zero)
        {
            return dt.UtcDateTime.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
        return dt.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
            System.Globalization.CultureInfo.InvariantCulture
        );
    }

    /// <summary>
    /// Writes a CBOR tag (major type 6).
    /// </summary>
    private void WriteTag(ulong tag)
    {
        WriteMajorTypeArgument(tag, 0xc0);
    }

    public void WriteDateTime(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                $"Only DateTimeKind.Utc is supported for CBOR serialization. Got {dt.Kind}. "
                    + "Use DateTimeOffset for values with a specific offset, or call DateTime.ToUniversalTime().",
                nameof(dt)
            );
        WriteTag(0);
        WriteString(
            dt.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                System.Globalization.CultureInfo.InvariantCulture
            )
        );
    }

    /// <summary>
    /// Writes an unsigned integer argument for a CBOR major type (2-5) with the given length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteMajorTypeArgument(ulong length, byte majorType)
    {
        if (length <= 0x17)
        {
            WriteByte((byte)(majorType + length));
        }
        else if (length <= byte.MaxValue)
        {
            WriteByte((byte)(majorType + 0x18));
            WriteByte((byte)length);
        }
        else if (length <= ushort.MaxValue)
        {
            WriteByte((byte)(majorType + 0x19));
            WriteBigEndian((ushort)length);
        }
        else if (length <= uint.MaxValue)
        {
            WriteByte((byte)(majorType + 0x1a));
            WriteBigEndian((uint)length);
        }
        else
        {
            WriteByte((byte)(majorType + 0x1b));
            WriteBigEndian(length);
        }
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        WriteMajorTypeArgument((uint)bytes.Length, 0x40);
        _out.WriteBytes(bytes);
    }

    public void WriteI8(sbyte b) => WriteI64(b);

    private static readonly Encoding s_utf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true
    );

    public void WriteString(string s)
    {
        var byteCount = s_utf8.GetByteCount(s);
        WriteMajorTypeArgument((uint)byteCount, 0x60);
        _out.WriteString(s, byteCount);
    }

    private void WriteUtf8(ReadOnlySpan<byte> str)
    {
        WriteMajorTypeArgument((uint)str.Length, 0x60);
        _out.WriteBytes(str);
    }

    ITypeSerializer ISerializer.WriteType(ISerdeInfo typeInfo) =>
        throw new NotSupportedException(
            "WriteType(ISerdeInfo) is not supported. Use WriteType(ISerdeInfo, int) instead."
        );

    ITypeSerializer ISerializer.WriteType(ISerdeInfo typeInfo, int fieldCount)
    {
        switch (typeInfo.Kind)
        {
            case InfoKind.CustomType:
                // Custom types are serialized as maps.
                BeginMap(fieldCount);
                return this;
            case InfoKind.Union:
                BeginMap(1);
                return this;
        }
        throw new InvalidOperationException("Unexpected info kind: " + typeInfo.Kind);
    }

    // Enums are serialized as a CBOR text string of the variant name (the field name at
    // the given ordinal in the enum's SerdeInfo), matching serde's name-based enum model.
    public void WriteEnum(ISerdeInfo info, int ordinal)
    {
        WriteUtf8(info.GetFieldName(ordinal));
    }

    public void WriteU16(ushort u16) => WriteU64(u16);

    public void WriteU32(uint u32) => WriteU64(u32);

    void ISerializer.WriteU64(ulong u64) => WriteU64(u64);

    private void WriteU64(ulong u64)
    {
        WriteMajorTypeArgument(u64, 0x00);
    }

    private void WriteBigEndian(ushort value)
    {
        Span<byte> span = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        _out.WriteBytes(span);
    }

    private void WriteBigEndian(uint value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        _out.WriteBytes(span);
    }

    private void WriteBigEndian(ulong value)
    {
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(span, value);
        _out.WriteBytes(span);
    }

    private void WriteBigEndian(float value)
    {
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(span, value);
        _out.WriteBytes(span);
    }

    private void WriteBigEndian(double value)
    {
        Span<byte> span = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(span, value);
        _out.WriteBytes(span);
    }
}
