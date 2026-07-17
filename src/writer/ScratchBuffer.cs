using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Serde.Cbor;

internal sealed class ScratchBuffer : IBufferWriter<byte>, IByteWriter, IDisposable
{
    private static readonly ArrayPool<byte> s_arrayPool = ArrayPool<byte>.Create();
    private static readonly Encoding s_utf8 = new UTF8Encoding(false, true);

    private byte[]? _rented;
    private int _count;

    public ScratchBuffer(int initialCapacity = 64)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }

        _rented = s_arrayPool.Rent(initialCapacity);
    }

    public int Count => _count;

    public int Capacity => _rented?.Length ?? 0;

    public ReadOnlyMemory<byte> WrittenMemory => (_rented ?? []).AsMemory(0, _count);

    public ReadOnlySpan<byte> WrittenSpan => WrittenMemory.Span;

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        EnsureCapacity(checked(_count + Math.Max(sizeHint, 1)));
        return _rented!.AsSpan(_count);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        EnsureCapacity(checked(_count + Math.Max(sizeHint, 1)));
        return _rented!.AsMemory(_count);
    }

    public void Advance(int count)
    {
        if ((uint)count > (uint)(Capacity - _count))
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _count += count;
    }

    public void WriteByte(byte value)
    {
        var buffer = _rented!;
        var count = _count;
        if ((uint)count < (uint)buffer.Length)
        {
            buffer[count] = value;
            _count = count + 1;
            return;
        }

        WriteByteSlow(value);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes) => WriteBytes(bytes.Span);

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(GetSpan(bytes.Length));
        Advance(bytes.Length);
    }

    public void Clear()
    {
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteByteSlow(byte value)
    {
        EnsureCapacity(checked(_count + 1));
        _rented![_count++] = value;
    }

    public void WriteString(string value, int maxByteCount = -1)
    {
        if (maxByteCount < 0)
        {
            maxByteCount = s_utf8.GetMaxByteCount(value.Length);
        }
        var span = GetSpan(maxByteCount);
        Advance(s_utf8.GetBytes(value, span));
    }

    private void EnsureCapacity(int capacity)
    {
        if (Capacity < capacity)
        {
            Grow(capacity);
        }
    }

    private void Grow(int capacity)
    {
        var newArray = s_arrayPool.Rent(Math.Max(capacity, Capacity * 2));
        WrittenSpan.CopyTo(newArray);
        s_arrayPool.Return(_rented!);
        _rented = newArray;
    }

    public void Dispose()
    {
        if (_rented is not null)
        {
            s_arrayPool.Return(_rented);
            _rented = null;
        }
        _count = 0;
    }
}

internal readonly struct SpecializedBuffer(ScratchBuffer _buffer) : IByteWriter
{
    public void WriteByte(byte b)
    {
        _buffer.WriteByte(b);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        _buffer.WriteBytes(bytes);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _buffer.WriteBytes(bytes);
    }

    public void WriteString(string value, int maxByteCount = -1)
    {
        _buffer.WriteString(value, maxByteCount);
    }
}
