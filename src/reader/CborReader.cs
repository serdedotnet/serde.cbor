

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
        if (b == 0xf4)
        {
            return false;
        }
        if (b == 0xf5)
        {
            return true;
        }
        throw new DeserializeException($"Expected boolean (0xf4/0xf5), got 0x{b:x}");
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
    /// Decodes the CBOR additional info value from an initial byte.
    /// The initial byte has already been consumed; this reads 0-8 more bytes as needed.
    /// </summary>
    private ulong ReadCborAdditionalInfo(byte initialByte)
    {
        var additional = initialByte & 0x1f;
        return additional switch
        {
            <= 23 => (ulong)additional,
            24 => EatByteOrThrow(),
            25 => ReadBigEndianU16(),
            26 => ReadBigEndianU32(),
            27 => ReadBigEndianU64(),
            _ => throw new DeserializeException($"Invalid CBOR additional info: {additional}")
        };
    }

    byte IDeserializer.ReadU8() => ReadU8();

    private byte ReadU8()
    {
        var b = EatByteOrThrow();
        if ((b >> 5) != 0)
            throw new DeserializeException($"Expected unsigned integer (major type 0), got 0x{b:x}");
        var val = ReadCborAdditionalInfo(b);
        if (val > byte.MaxValue)
            throw new DeserializeException($"Value {val} out of range for byte");
        return (byte)val;
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
            // CBOR major type 4: array
            if ((b >> 5) != 4)
                throw new DeserializeException($"Expected array (major type 4), got 0x{b:x}");
            if (b == 0x9f)
                throw new DeserializeException("Indefinite-length arrays are not supported");
            int length = checked((int)ReadCborAdditionalInfo(b));
            return new DeserializeCollection(this, false, length);
        }
        else if (typeInfo.Kind == InfoKind.Dictionary)
        {
            // CBOR major type 5: map
            if ((b >> 5) != 5)
                throw new DeserializeException($"Expected map (major type 5), got 0x{b:x}");
            if (b == 0xbf)
                throw new DeserializeException("Indefinite-length maps are not supported");
            int length = checked((int)ReadCborAdditionalInfo(b));
            return new DeserializeCollection(this, true, length * 2);
        }
        else
        {
            throw new DeserializeException("Expected either List or Dictionary, found " + typeInfo.Kind);
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
            span = RefillNoEof(9);
        }
        var b = span[0];
        if (b != 0xfb)
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
        if (b != 0xfa)
        {
            throw new Exception($"Expected 32-bit float, got 0x{b:x}");
        }
        var result = BinaryPrimitives.ReadSingleBigEndian(span[1..]);
        _reader.Advance(5);
        return result;
    }
    float IDeserializer.ReadF32() => ReadF32();

    /// <summary>
    /// Reads a CBOR integer (major type 0 or 1) and returns the signed value.
    /// Major type 0: positive, value = additional info.
    /// Major type 1: negative, value = -1 - additional info.
    /// </summary>
    private long ReadCborSignedInteger()
    {
        var b = EatByteOrThrow();
        var majorType = b >> 5;
        if (majorType == 0)
        {
            var val = ReadCborAdditionalInfo(b);
            if (val > (ulong)long.MaxValue)
                throw new DeserializeException($"Unsigned value {val} too large for Int64");
            return (long)val;
        }
        if (majorType == 1)
        {
            var n = ReadCborAdditionalInfo(b);
            if (n > (ulong)long.MaxValue)
                throw new DeserializeException($"Negative value too large for Int64");
            return -1 - (long)n;
        }
        throw new DeserializeException($"Expected integer (major type 0 or 1), got 0x{b:x}");
    }

    public short ReadI16()
    {
        var val = ReadCborSignedInteger();
        if (val < short.MinValue || val > short.MaxValue)
            throw new DeserializeException($"Value {val} out of range for Int16");
        return (short)val;
    }

    private int ReadI32()
    {
        var val = ReadCborSignedInteger();
        if (val < int.MinValue || val > int.MaxValue)
            throw new DeserializeException($"Value {val} out of range for Int32");
        return (int)val;
    }

    int IDeserializer.ReadI32() => ReadI32();

    private long ReadI64() => ReadCborSignedInteger();

    long IDeserializer.ReadI64() => ReadI64();

    T? IDeserializer.ReadNullableRef<T>(IDeserialize<T> proxy)
        where T : class
    {
        if (((IDeserializer)this).TryReadNull())
        {
            return null;
        }
        return proxy.Deserialize(this);
    }

    bool IDeserializer.TryReadNull()
    {
        var b = PeekByteOrThrow();
        if (b == 0xf6)
        {
            _reader.Advance(1);
            return true;
        }
        return false;
    }

    // Enums are encoded as a CBOR text string of the variant name. Read the name and map it
    // back to the variant ordinal via the enum's SerdeInfo.
    int IDeserializer.ReadEnum(ISerdeInfo info)
    {
        var span = ReadUtf8Span();
        int index = info.TryGetIndex(span);
        if (index == ITypeDeserializer.IndexNotFound)
        {
            throw new DeserializeException(
                $"Unknown enum member '{Encoding.UTF8.GetString(span)}' for enum '{info.Name}'"
            );
        }
        return index;
    }

    public sbyte ReadI8()
    {
        var val = ReadCborSignedInteger();
        if (val < sbyte.MinValue || val > sbyte.MaxValue)
            throw new DeserializeException($"Value {val} out of range for sbyte");
        return (sbyte)val;
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
        // Read as DateTimeOffset, then return UTC DateTime
        return ReadDateTimeOffset().UtcDateTime;
    }

    public DateTimeOffset ReadDateTimeOffset()
    {
        var b = PeekByteOrThrow();
        var majorType = b >> 5;
        if (majorType != 6)
            throw new DeserializeException($"Expected tag (major type 6) for DateTimeOffset, got 0x{b:x}");

        EatByteOrThrow();
        var tag = ReadCborAdditionalInfo(b);

        if (tag == 0)
        {
            // Tag 0: RFC 3339 date/time string
            var s = ReadString();
            return DateTimeOffset.Parse(s,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind);
        }
        else if (tag == 1)
        {
            // Tag 1: epoch-based date/time (integer or float seconds)
            var next = PeekByteOrThrow();
            var nextMajor = next >> 5;

            if (nextMajor == 0)
            {
                // Unsigned integer seconds
                var seconds = (long)ReadCborUnsigned();
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
            else if (nextMajor == 1)
            {
                // Negative integer seconds
                var val = ReadCborSignedInteger();
                return DateTimeOffset.FromUnixTimeSeconds(val);
            }
            else if (next == 0xfb)
            {
                // Double-precision float seconds
                var seconds = ReadF64();
                if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                    throw new DeserializeException("Tag 1 date/time cannot be NaN or Infinity");
                long wholeSec = (long)Math.Truncate(seconds);
                double frac = seconds - wholeSec;
                long ticks = DateTimeOffset.FromUnixTimeSeconds(wholeSec).Ticks
                    + (long)Math.Round(frac * TimeSpan.TicksPerSecond);
                return new DateTimeOffset(ticks, TimeSpan.Zero);
            }
            else if (next == 0xfa)
            {
                // Single-precision float seconds
                var seconds = (double)ReadF32();
                if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                    throw new DeserializeException("Tag 1 date/time cannot be NaN or Infinity");
                long wholeSec = (long)Math.Truncate(seconds);
                double frac = seconds - wholeSec;
                long ticks = DateTimeOffset.FromUnixTimeSeconds(wholeSec).Ticks
                    + (long)Math.Round(frac * TimeSpan.TicksPerSecond);
                return new DateTimeOffset(ticks, TimeSpan.Zero);
            }
            else
            {
                throw new DeserializeException($"Tag 1 expects integer or float, got 0x{next:x}");
            }
        }
        else
        {
            throw new DeserializeException($"Expected tag 0 or 1 for DateTimeOffset, got tag {tag}");
        }
    }

    public UInt128 ReadU128()
    {
        throw new NotImplementedException("128-bit integers are not yet supported in CBOR serialization.");
    }

    public Int128 ReadI128()
    {
        throw new NotImplementedException("128-bit integers are not yet supported in CBOR serialization.");
    }

    public void ReadBytes(IBufferWriter<byte> writer)
    {
        var b = EatByteOrThrow();
        int length = b switch
        {
            >= 0x40 and <= 0x57 => b - 0x40,
            0x58 => EatByteOrThrow(),
            0x59 => ReadBigEndianU16(), // 16-bit length
            0x5a => checked((int)ReadBigEndianU32()), // 32-bit length
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
        switch (typeInfo.Kind)
        {
            case InfoKind.List:
            case InfoKind.Dictionary:
                return ReadCollection(typeInfo);
            case InfoKind.CustomType:
                // Custom types are serialized as a map
                int? length = ReadMapLength();
                return new DeserializeType(this, length);
            default:
                throw new ArgumentException("Unexpected info kind: " + typeInfo.Kind);
        }
    }

    private int ReadMapLength()
    {
        var b = EatByteOrThrow();
        int length;
        if (b is >= 0xa0 and <= 0xb7)
        {
            length = b - 0xa0;
        }
        else if (b == 0xb8)
        {
            length = EatByteOrThrow();
        }
        else if (b == 0xb9)
        {
            length = ReadBigEndianU16();
        }
        else if (b == 0xba)
        {
            length = (int)ReadBigEndianU32();
        }
        else
        {
            throw new DeserializeException($"Expected map, got 0x{b:x}");
        }
        return length;
    }

    /// <summary>
    /// Reads a CBOR unsigned integer (major type 0).
    /// </summary>
    private ulong ReadCborUnsigned()
    {
        var b = EatByteOrThrow();
        if ((b >> 5) != 0)
            throw new DeserializeException($"Expected unsigned integer (major type 0), got 0x{b:x}");
        return ReadCborAdditionalInfo(b);
    }

    private ushort ReadU16()
    {
        var val = ReadCborUnsigned();
        if (val > ushort.MaxValue)
            throw new DeserializeException($"Value {val} out of range for UInt16");
        return (ushort)val;
    }

    ushort IDeserializer.ReadU16() => ReadU16();

    public uint ReadU32()
    {
        var val = ReadCborUnsigned();
        if (val > uint.MaxValue)
            throw new DeserializeException($"Value {val} out of range for UInt32");
        return (uint)val;
    }

    public ulong ReadU64() => ReadCborUnsigned();

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