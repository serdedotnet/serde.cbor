

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Serde.IO;

namespace Serde.Cbor;

internal sealed partial class CborReader<TReader> : IDeserializer
    where TReader : IBufReader
{
    private TReader _reader;

    public CborReader(TReader reader)
    {
        _reader = reader;
        // start with a filled buffer
        _reader.FillBuffer(1);
    }

    void IDisposable.Dispose()
    { }

    [DoesNotReturn]
    private static void ThrowEof()
    {
        throw new Exception("Unexpected end of stream");
    }

    bool IDeserializer.ReadBool() => ReadBool();

    private bool ReadBool()
    {
        var b = EatByteOrThrow();
        if (b == 0xc2)
        {
            return false;
        }
        if (b == 0xc3)
        {
            return true;
        }
        throw new Exception($"Expected boolean, got 0x{b:x}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadOnlySpan<byte> RefillNoEof(int fillCount)
    {
        if (!_reader.FillBuffer(fillCount))
        {
            ThrowEof();
        }
        return _reader.Span;
    }

    private byte PeekByteOrThrow()
    {
        var span = _reader.Span;
        if (span.Length == 0)
        {
            span = RefillNoEof(1);
        }
        return span[0];
    }

    private byte EatByteOrThrow()
    {
        var result = PeekByteOrThrow();
        _reader.Advance(1);
        return result;
    }

    /// <summary>
    /// Eats at least one byte from the buffer.
    /// </summary>
    bool TryReadU8(out byte result)
    {
        return TryReadByte(EatByteOrThrow(), out result);
    }

    bool TryReadByte(byte b, out byte result)
    {
        if (b <= 0x17)
        {
            result = b;
            return true;
        }
        if (b == 0x18)
        {
            result = EatByteOrThrow();
            return true;
        }
        result = b;
        return false;
    }

    byte IDeserializer.ReadU8() => ReadU8();

    private byte ReadU8()
    {
        if (!TryReadU8(out var b))
        {
            throw new Exception($"Expected byte 0xcc, got 0x{b:x}");
        }
        return b;
    }

    public char ReadChar()
    {
        // char is encoded as a string
        return ReadString().Single();
    }

    private ITypeDeserializer ReadCollection(ISerdeInfo typeInfo)
    {
        var b = EatByteOrThrow();
        if (typeInfo.Kind == InfoKind.List)
        {
            int length;
            if (b <= 0x9f)
            {
                length = b & 0xf;
            }
            else if (b == 0xdc)
            {
                length = ReadBigEndianU16();
            }
            else if (b == 0xdd)
            {
                length = (int)ReadBigEndianU32();
            }
            else
            {
                throw new Exception($"Expected array, got 0x{b:x}");
            }
            return new DeserializeCollection(this, false, length);
        }
        else if (typeInfo.Kind == InfoKind.Dictionary)
        {
            int length;
            if (b <= 0x8f)
            {
                length = b & 0xf;
            }
            else if (b == 0xde)
            {
                length = ReadBigEndianU16();
            }
            else if (b == 0xdf)
            {
                length = (int)ReadBigEndianU32();
            }
            else
            {
                throw new Exception($"Expected dictionary, got 0x{b:x}");
            }
            return new DeserializeCollection(this, true, length*2);
        }
        else
        {
            throw new Exception("Expected either List or Dictionary, found " + typeInfo.Kind);
        }
    }

    public decimal ReadDecimal()
    {
        throw new NotImplementedException();
    }

    private double ReadF64()
    {
        var span = _reader.Span;
        if (span.Length < 9)
        {
            span = RefillNoEof(0);
        }
        var b = span[0];
        if (b != 0xcb)
        {
            throw new Exception($"Expected 64-bit double, got 0x{b:x}");
        }
        var result = BinaryPrimitives.ReadDoubleBigEndian(span[1..]);
        _reader.Advance(9);
        return result;
    }

    double IDeserializer.ReadF64() => ReadF64();

    private float ReadF32()
    {
        var span = _reader.Span;
        if (span.Length < 5)
        {
            span = RefillNoEof(5);
        }
        var b = span[0];
        if (b != 0xca)
        {
            throw new Exception($"Expected 32-bit float, got 0x{b:x}");
        }
        var result = BinaryPrimitives.ReadSingleBigEndian(span[1..]);
        _reader.Advance(5);
        return result;
    }
    float IDeserializer.ReadF32() => ReadF32();

    private bool TryReadI8(out sbyte s)
    {
        var first = EatByteOrThrow();
        if (first is (>= 0 and <= 0x17) or (>= 0x20 and <= 0x37))
        {
            s = (sbyte)first;
            return true;
        }
        if (first == 0x38)
        {
            s = (sbyte)EatByteOrThrow();
            return true;
        }
        s = (sbyte)first;
        return false;
    }

    private bool TryReadI16(out short i16)
    {
        if (TryReadI8(out var sb))
        {
            i16 = sb;
            return true;
        }
        if (TryReadByte((byte)sb, out var b))
        {
            i16 = b;
            return true;
        }
        if (b == 0x19)
        {
            var u16 = ReadBigEndianU16();
            if (u16 > short.MaxValue)
            {
                throw new DeserializeException($"Expected Int16, got {u16}");
            }
            i16 = unchecked((short)u16);
            return true;
        }
        if (b == 0x39)
        {
            i16 = unchecked((short)ReadBigEndianU16());
            if (i16 > 0)
            {
                throw new DeserializeException($"Expected negative Int16, got {i16}");
            }
            return true;
        }
        i16 = b;
        return false;
    }

    public short ReadI16()
    {
        if (!TryReadI16(out var i16))
        {
            throw new Exception("Expected 16-bit integer");
        }
        return i16;
    }

    private bool TryReadI32(out int i32)
    {
        if (TryReadI16(out var i16))
        {
            i32 = i16;
            return true;
        }
        switch (i16)
        {
            default:
                i32 = i16;
                return false;

            case 0x19:
                i32 = ReadBigEndianU16();
                return true;
            case 0x1a: // four byte uint32
                var u32 = ReadBigEndianU32();
                if (u32 > int.MaxValue)
                {
                    throw new DeserializeException($"Expected Int32, got {u32}");
                }
                i32 = unchecked((int)u32);
                return true;
            case 0x3a: // four byte negative int32
                i32 = unchecked((int)ReadBigEndianU32());
                if (i32 > 0)
                {
                    throw new DeserializeException($"Expected negative Int32, got {i32}");
                }
                return true;
        }
    }

    private int ReadI32()
    {
        if (!TryReadI32(out var i32))
        {
            throw new Exception($"Expected 32-bit integer, found 0x{i32:x}");
        }
        return i32;
    }

    int IDeserializer.ReadI32() => ReadI32();

    private bool TryReadI64(out long i64)
    {
        if (TryReadI32(out var i32))
        {
            i64 = i32;
            return true;
        }
        if (i32 == 0x1a)
        {
            i64 = ReadBigEndianU32();
            return true;
        }
        if (i32 == 0x1b)
        {
            i64 = (long)ReadBigEndianU64();
            return true;
        }
        i64 = i32;
        return false;
    }

    private long ReadI64()
    {
        if (!TryReadI64(out var i64))
        {
            throw new Exception("Expected 64-bit integer");
        }
        return i64;
    }

    long IDeserializer.ReadI64() => ReadI64();

    T? IDeserializer.ReadNullableRef<T>(IDeserialize<T> proxy)
        where T : class
    {
        var b = PeekByteOrThrow();
        if (b == 0xc0)
        {
            _reader.Advance(1);
            return null;
        }
        return proxy.Deserialize(this);
    }

    public sbyte ReadI8()
    {
        if (!TryReadI8(out var sb))
        {
            throw new Exception("Expected signed byte");
        }
        return sb;
    }

    string IDeserializer.ReadString() => ReadString();

    private string ReadString()
    {
        // strings are encoded in UTF8 as byte arrays
        var span = ReadUtf8Span();
        var str = Encoding.UTF8.GetString(span);
        return str;
    }

    public DateTime ReadDateTime()
    {
        return DateTime.Parse(ReadString(), styles: DateTimeStyles.RoundtripKind);
    }

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        var b = EatByteOrThrow();
        int length = b switch
        {
            0xc4 => EatByteOrThrow(),
            0xc5 => ReadBigEndianU16(), // 16-bit length
            0xc6 => checked((int)ReadBigEndianU32()), // 32-bit length
            _ => throw new DeserializeException($"Expected bytes, got 0x{b:x}"),
        };
        if (!_reader.FillBuffer(length))
        {
            ThrowEof();
        }
        var inputSpan = _reader.Span[..length];
        var outSpan = writer.GetSpan(length);
        inputSpan.CopyTo(outSpan);
        _reader.Advance(length);
        writer.Advance(length);
    }

    private ReadOnlySpan<byte> ReadUtf8Span()
    {
        var b = EatByteOrThrow();
        int length;
        if (b is (>= 0x60 and <= 0x77))
        {
            length = b - 0x60;
        }
        else if (b == 0x78)
        {
            // 8-bit length
            length = EatByteOrThrow();
        }
        else if (b == 0x79)
        {
            // 16-bit length
            length = ReadBigEndianU16();
        }
        else if (b == 0x7a)
        {
            // 32-bit length
            length = (int)ReadBigEndianU32();
        }
        else if (b == 0x7b)
        {
            throw new DeserializeException("Found 8-byte length string, maximum length is 4 byte");
        }
        else
        {
            throw new DeserializeException($"Expected string, got 0x{b:x}");
        }
        var span = _reader.Span;
        if (span.Length < length)
        {
            span = RefillNoEof(length);
        }
        _reader.Advance(length);
        return span[..length];
    }

    ITypeDeserializer IDeserializer.ReadType(ISerdeInfo typeInfo)
    {
        if (typeInfo.Kind == InfoKind.List || typeInfo.Kind == InfoKind.Dictionary)
        {
            return ReadCollection(typeInfo);
        }
        else if (typeInfo.Kind == InfoKind.CustomType)
        {
            var fieldCount = typeInfo.FieldCount;
            var b = EatByteOrThrow();
            int length;
            if (fieldCount <= 0xff)
            {
                if (b > 0x9f)
                {
                    throw new Exception($"Expected array, got 0x{b:x}");
                }
                length = b & 0xf;
            }
            else if (fieldCount <= 0xffff)
            {
                if (b != 0xdc)
                {
                    throw new Exception($"Expected 16-bit array, got 0x{b:x}");
                }
                length = ReadBigEndianU16();
            }
            else
            {
                if (b != 0xdd)
                {
                    throw new Exception($"Expected 32-bit array, got 0x{b:x}");
                }
                length = (int)ReadBigEndianU32();
            }
            if (length != fieldCount)
            {
                throw new Exception($"Expected array of length {fieldCount}, got {length}");
            }
            return new DeserializeType(this);
        }
        else if (typeInfo.Kind == InfoKind.Enum)
        {
            return new DeserializeType(this);
        }
        throw new Exception("Expected custom type or enum");
    }

    private ushort ReadU16()
    {
        if (TryReadU16(out var u16))
        {
            return u16;
        }
        throw new Exception($"Expected 16-bit positive integer, got 0x{u16:x}");
    }

    ushort IDeserializer.ReadU16() => ReadU16();

    private bool TryReadU16(out ushort u16)
    {
        if (TryReadU8(out var b))
        {
            u16 = b;
            return true;
        }
        if (b == 0x19)
        {
            u16 = ReadBigEndianU16();
            return true;
        }
        u16 = b;
        return false;
    }

    private bool TryReadU32(out uint u32)
    {
        if (TryReadU16(out var u16))
        {
            u32 = u16;
            return true;
        }
        // u16 contains the first unexpected byte
        if (u16 == 0xce)
        {
            u32 = ReadBigEndianU32();
            return true;
        }
        u32 = u16;
        return false;
    }

    private bool TryReadU64(out ulong u64)
    {
        if (TryReadU32(out var u32))
        {
            u64 = u32;
            return true;
        }
        // u32 contains the first unexpected byte
        if (u32 == 0xcf)
        {
            u64 = ReadBigEndianU64();
            return true;
        }
        u64 = u32;
        return false;
    }

    public uint ReadU32()
    {
        if (!TryReadU32(out var u32))
        {
            throw new Exception($"Expected integer, got 0x{u32:x}");
        }
        return u32;
    }

    public ulong ReadU64()
    {
        if (!TryReadU64(out var u64))
        {
            throw new Exception($"Expected integer, got 0x{u64:x}");
        }
        return u64;
    }

    private ushort ReadBigEndianU16()
    {
        var span = _reader.Span;
        if (span.Length < 2)
        {
            span = RefillNoEof(2);
        }
        var result = BinaryPrimitives.ReadUInt16BigEndian(span);
        _reader.Advance(2);
        return result;
    }

    private uint ReadBigEndianU32()
    {
        var span = _reader.Span;
        if (span.Length < 4)
        {
            span = RefillNoEof(4);
        }
        var result = BinaryPrimitives.ReadUInt32BigEndian(span);
        _reader.Advance(4);
        return result;
    }

    private ulong ReadBigEndianU64()
    {
        var span = _reader.Span;
        if (span.Length < 8)
        {
            span = RefillNoEof(8);
        }
        var result = BinaryPrimitives.ReadUInt64BigEndian(span);
        _reader.Advance(8);
        return result;
    }
}