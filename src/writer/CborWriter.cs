
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serde.Cbor;

internal sealed partial class CborWriter : ISerializer
{
    private readonly ScratchBuffer _out;
    private readonly EnumSerializer _enumSerializer;

    public CborWriter(ScratchBuffer scratch)
    {
        _out = scratch;
        _enumSerializer = new EnumSerializer(this);
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
            if (length <= 15)
            {
                _out.Add((byte)(0x90 | length));
            }
            else if (length <= 0xffff)
            {
                _out.Add(0xdc);
                WriteBigEndian((ushort)length);
            }
            else
            {
                _out.Add(0xdd);
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
        if (length <= 15)
        {
            _out.Add((byte)(0x80 | length));
        }
        else if (length <= 0xffff)
        {
            _out.Add(0xde);
            WriteBigEndian((ushort)length);
        }
        else
        {
            _out.Add(0xdf);
            WriteBigEndian((uint)length);
        }
    }

    public void WriteDecimal(decimal d)
    {
        throw new NotImplementedException();
    }

    public void WriteF64(double d)
    {
        _out.Add(0xcb);
        WriteBigEndian(d);
    }

    public void WriteF32(float f)
    {
        _out.Add(0xca);
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
        else if (i64 >= -0x17)
        {
            _out.Add(unchecked((byte)i64));
        }
        else if (i64 >= sbyte.MinValue)
        {
            _out.Add(0x38);
            _out.Add(unchecked((byte)i64));
        }
        else if (i64 >= short.MinValue)
        {
            _out.Add(0x39);
            WriteBigEndian(unchecked((short)i64));
        }
        else if (i64 >= int.MinValue)
        {
            _out.Add(0x3a);
            WriteBigEndian(unchecked((int)i64));
        }
        else
        {
            _out.Add(0x3b);
            WriteBigEndian(i64);
        }
    }
    public void WriteNull()
    {
        _out.Add(0xc0);
    }

    public void WriteDateTimeOffset(DateTimeOffset dt)
    {
        WriteString(dt.ToString("O"));
    }

    public void WriteDateTime(DateTime dt)
    {
        WriteString(dt.ToString("O"));
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        byte code = bytes.Length switch
        {
            <= 0xff => 0xc4,
            <= 0xffff => 0xc5,
            _ => 0xc6
        };
        var prefixLen = code switch
        {
            0xc4 => 2,
            0xc5 => 3,
            _ => 5
        };
        var span = _out.GetAppendSpan(prefixLen + bytes.Length);
        _out.Count += prefixLen + bytes.Length;
        span[0] = code;
        switch (prefixLen)
        {
            case 2:
                span[1] = (byte)bytes.Length;
                break;
            case 3:
                WriteBigEndian((ushort)bytes.Length);
                break;
            case 5:
                WriteBigEndian((uint)bytes.Length);
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
                // Custom types are serialized as a map
                WriteMapLength(typeInfo.FieldCount);
                return this;
            case InfoKind.Enum:
                return _enumSerializer;
        }
        throw new InvalidOperationException("Unexpected info kind: " + typeInfo.Kind);
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