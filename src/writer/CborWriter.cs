
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serde.Cbor;

internal sealed partial class CborWriter : ISerializer
{
    private readonly ScratchBuffer _out;

    // Stack of in-progress custom-type map headers. Because the generated serializer
    // may omit fields (e.g. WriteStringIfNotNull skips null members), the number of
    // pairs actually written is not known when the header is emitted. We reserve a
    // placeholder header, count the fields as they are written, and backpatch the real
    // count in End. Frames form a strict LIFO stack for nested custom types.
    private struct MapFrame
    {
        public int Offset;
        public int Width;
        public int Count;
    }
    private MapFrame[] _mapFrames = new MapFrame[4];
    private int _mapDepth;

    public CborWriter(ScratchBuffer scratch)
    {
        _out = scratch;
    }

    public void WriteBool(bool b)
    {
        _out.Add((byte)(b ? 0xf5 : 0xf4));
    }

    public void WriteU8(byte b) => WriteU64(b);

    public void WriteChar(char c) => WriteString(c.ToString());

    ITypeSerializer ISerializer.WriteCollection(ISerdeInfo typeInfo, int? length)
    {
        if (length is null)
        {
            throw new InvalidOperationException("Cannot serialize a collection with an unknown length.");
        }
        if (typeInfo.Kind == InfoKind.List)
        {
            if (length <= 0x17)
            {
                _out.Add((byte)(0x80 + length));
            }
            else if (length <= byte.MaxValue)
            {
                _out.Add(0x98);
                _out.Add((byte)length);
            }
            else if (length <= ushort.MaxValue)
            {
                _out.Add(0x99);
                WriteBigEndian((ushort)length);
            }
            else
            {
                _out.Add(0x9a);
                WriteBigEndian((uint)length);
            }
        }
        else if (typeInfo.Kind == InfoKind.Dictionary)
        {
            WriteMapLength((int)length);
        }
        else
        {
            throw new InvalidOperationException("Expected a collection, found: " + typeInfo.Kind);
        }
        return new SerCollection(this);
    }

    private void WriteMapLength(int length)
    {
        if (length <= 0x17)
        {
            _out.Add((byte)(0xa0 + length));
        }
        else if (length <= byte.MaxValue)
        {
            _out.Add(0xb8);
            _out.Add((byte)length);
        }
        else if (length <= ushort.MaxValue)
        {
            _out.Add(0xb9);
            WriteBigEndian((ushort)length);
        }
        else
        {
            _out.Add(0xba);
            WriteBigEndian((uint)length);
        }
    }

    /// <summary>
    /// Reserves space for a definite-length map header sized to hold up to
    /// <paramref name="maxLength"/> pairs and pushes a frame. The actual pair count is
    /// backpatched by <see cref="EndMap"/>. Fields may be skipped by the serializer, so
    /// the final count is only known once all fields have been written.
    /// </summary>
    private void BeginMap(int maxLength)
    {
        int width = maxLength switch
        {
            <= 0x17 => 1,
            <= byte.MaxValue => 2,
            <= ushort.MaxValue => 3,
            _ => 5
        };
        int offset = _out.Count;
        _out.GetAppendSpan(width);
        _out.Count += width;
        if (_mapDepth == _mapFrames.Length)
        {
            Array.Resize(ref _mapFrames, _mapFrames.Length * 2);
        }
        _mapFrames[_mapDepth] = new MapFrame { Offset = offset, Width = width, Count = 0 };
        _mapDepth++;
    }

    private void IncrementMapCount()
    {
        _mapFrames[_mapDepth - 1].Count++;
    }

    /// <summary>
    /// Pops the current map frame and writes the actual pair count into the reserved
    /// header. The count is guaranteed to fit in the reserved width since it can never
    /// exceed the type's field count.
    /// </summary>
    private void EndMap()
    {
        _mapDepth--;
        var frame = _mapFrames[_mapDepth];
        var span = _out.BufferSpan.Slice(frame.Offset, frame.Width);
        int count = frame.Count;
        switch (frame.Width)
        {
            case 1:
                span[0] = (byte)(0xa0 + count);
                break;
            case 2:
                span[0] = 0xb8;
                span[1] = (byte)count;
                break;
            case 3:
                span[0] = 0xb9;
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)count);
                break;
            default:
                span[0] = 0xba;
                BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)count);
                break;
        }
    }

    public void WriteDecimal(decimal d)
    {
        throw new NotImplementedException();
    }

    public void WriteU128(UInt128 u128)
    {
        throw new NotImplementedException("128-bit integers are not yet supported in CBOR serialization.");
    }

    public void WriteI128(Int128 i128)
    {
        throw new NotImplementedException("128-bit integers are not yet supported in CBOR serialization.");
    }

    public void WriteF64(double d)
    {
        _out.Add(0xfb);
        WriteBigEndian(d);
    }

    public void WriteF32(float f)
    {
        _out.Add(0xfa);
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
            if (n <= 0x17)
            {
                _out.Add((byte)(0x20 + n));
            }
            else if (n <= byte.MaxValue)
            {
                _out.Add(0x38);
                _out.Add((byte)n);
            }
            else if (n <= ushort.MaxValue)
            {
                _out.Add(0x39);
                WriteBigEndian((ushort)n);
            }
            else if (n <= uint.MaxValue)
            {
                _out.Add(0x3a);
                WriteBigEndian((uint)n);
            }
            else
            {
                _out.Add(0x3b);
                WriteBigEndian(n);
            }
        }
    }
    public void WriteNull()
    {
        _out.Add(0xf6);
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
            return dt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                System.Globalization.CultureInfo.InvariantCulture);
        }
        return dt.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes a CBOR tag (major type 6).
    /// </summary>
    private void WriteTag(ulong tag)
    {
        // Major type 6 = 0xc0 base, same additional-info encoding as unsigned integers
        if (tag <= 0x17)
        {
            _out.Add((byte)(0xc0 + tag));
        }
        else if (tag <= 0xff)
        {
            _out.Add(0xd8);
            _out.Add((byte)tag);
        }
        else if (tag <= 0xffff)
        {
            _out.Add(0xd9);
            WriteBigEndian((ushort)tag);
        }
        else if (tag <= 0xffffffff)
        {
            _out.Add(0xda);
            WriteBigEndian((uint)tag);
        }
        else
        {
            _out.Add(0xdb);
            WriteBigEndian(tag);
        }
    }

    public void WriteDateTime(DateTime dt)
    {
        if (dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                $"Only DateTimeKind.Utc is supported for CBOR serialization. Got {dt.Kind}. " +
                "Use DateTimeOffset for values with a specific offset, or call DateTime.ToUniversalTime().",
                nameof(dt));
        WriteTag(0);
        WriteString(dt.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
            System.Globalization.CultureInfo.InvariantCulture));
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        var bytesLen = bytes.Length;
        (byte code, int prefixLen) = bytesLen switch
        {
            <= 0x17 => ((byte)(0x40 + bytesLen), 1),
            <= byte.MaxValue => ((byte)0x58, 2),
            <= ushort.MaxValue => ((byte)0x59, 3),
            _ => ((byte)0x5a, 5)
        };
        var span = _out.GetAppendSpan(prefixLen + bytesLen);
        _out.Count += prefixLen + bytesLen;
        span[0] = code;
        switch (prefixLen)
        {
            case 2:
                span[1] = (byte)bytesLen;
                break;
            case 3:
                BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)bytesLen);
                break;
            case 5:
                BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)bytesLen);
                break;
        }
        bytes.Span.CopyTo(span[prefixLen..]);
    }

    public void WriteI8(sbyte b) => WriteI64(b);

    private static readonly Encoding _utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public void WriteString(string s)
    {
        // We can write the string directly to the output buffer, but the string
        // is length-prefixed and we don't know precisely how long it will be until
        // we encode it. So we need to write space for the length prefix first, then
        // write the string, and finally go back, fill in the length prefix, and move
        // the string if necessary.
        var sLen = s.Length;
        var maxByteCount = _utf8.GetMaxByteCount(sLen);
        var appendSpan = _out.GetAppendSpan(checked(maxByteCount + 5 /* max length prefix */));
        int estimatedOffset = sLen switch {
            <= 0x17 => 1,
            <= byte.MaxValue => 2,
            <= ushort.MaxValue => 3,
            _ => 5
        };
        var u8Dest = appendSpan.Slice(estimatedOffset, maxByteCount);
        int actualStrSize = _utf8.GetBytes(s, u8Dest);
        // write prefix and move body if necessary
        int actualOffset = WriteUtf8Header(actualStrSize, appendSpan);
		if (actualOffset < estimatedOffset)
        {
            u8Dest.CopyTo(appendSpan.Slice(actualOffset, actualStrSize));
        }
        _out.Count += actualOffset + actualStrSize;
    }

    private void WriteUtf8(ReadOnlySpan<byte> str)
    {
        var span = _out.GetAppendSpan(str.Length + 5);
        int offset = WriteUtf8Header(str.Length, span);
        str.CopyTo(span.Slice(offset, str.Length));
        _out.Count += offset + str.Length;
    }

    /// <summary>
    /// Assumes that span is large enough to hold the header.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteUtf8Header(int length, Span<byte> span)
    {
        int offset;
        if (length <= 0x17)
        {
            offset = 1;
            span[0] = (byte)(0x60 + length);
        }
        else if (length <= byte.MaxValue)
        {
            offset = 2;
            span[0] = 0x78;
            span[1] = unchecked((byte)length);
        }
        else if (length <= ushort.MaxValue)
        {
            offset = 3;
            span[0] = 0x79;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(1), (ushort)length);
        }
        else
        {
            offset = 5;
            span[0] = 0x7a;
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(1), (uint)length);
        }
        return offset;
    }

    ITypeSerializer ISerializer.WriteType(ISerdeInfo typeInfo)
    {
        switch (typeInfo.Kind)
        {
            case InfoKind.CustomType:
                BeginMap(1);
                return this;
            case InfoKind.Union:
                // Custom types are serialized as maps. The pair count is backpatched in
                // End since the serializer may skip fields (e.g. null members).
                BeginMap(typeInfo.FieldCount);
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
        if (u64 <= 0x17)
        {
            _out.Add((byte)u64);
        }
        else if (u64 <= 0xff)
        {
            _out.Add(0x18);
            _out.Add((byte)u64);
        }
        else if (u64 <= 0xffff)
        {
            _out.Add(0x19);
            WriteBigEndian((ushort)u64);
        }
        else if (u64 <= 0xffffffff)
        {
            _out.Add(0x1a);
            WriteBigEndian((uint)u64);
        }
        else
        {
            _out.Add(0x1b);
            WriteBigEndian(u64);
        }
    }

    private void WriteBigEndian(ushort value)
    {
        var span = _out.GetAppendSpan(2);
        BinaryPrimitives.WriteUInt16BigEndian(
            span,
            value);
        _out.Count += 2;
    }

    private void WriteBigEndian(uint value)
    {
        var span = _out.GetAppendSpan(4);
        BinaryPrimitives.WriteUInt32BigEndian(
            span,
            value
        );
        _out.Count += 4;
    }

    private void WriteBigEndian(ulong value)
    {
        var span = _out.GetAppendSpan(8);
        BinaryPrimitives.WriteUInt64BigEndian(
            span,
            value
        );
        _out.Count += 8;
    }

    private void WriteBigEndian(short value) => WriteBigEndian((ushort)value);
    private void WriteBigEndian(int value) => WriteBigEndian((uint)value);
    private void WriteBigEndian(long value) => WriteBigEndian((ulong)value);
    private void WriteBigEndian(float value)
    {
        var span = _out.GetAppendSpan(4);
        BinaryPrimitives.WriteSingleBigEndian(span, value);
        _out.Count += 4;
    }
    private void WriteBigEndian(double value)
    {
        var span = _out.GetAppendSpan(8);
        BinaryPrimitives.WriteDoubleBigEndian(span, value);
        _out.Count += 8;
    }
}