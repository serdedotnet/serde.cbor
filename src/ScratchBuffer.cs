
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

internal sealed class ScratchBuffer : IDisposable
{
    public ScratchBuffer() { }
    public ScratchBuffer(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        _rented = ArrayPool<byte>.Shared.Rent(capacity);
    }

    private byte[]? _rented;
    private int _count;
    public int Count
    {
        get => _count;
        set
        {
            if ((uint)value >= _count && (uint)value <= _rented?.Length)
            {
                _count = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    public int Capacity => _rented?.Length ?? 0;

    /// <summary>
    /// The underlying buffer, including unused space at the end.
    /// </summary>
    public Span<byte> BufferSpan => _rented ?? default;

    /// <summary>
    /// A span of the used portion of the buffer.
    /// </summary>
    public Span<byte> Span => _count == 0 ? default : BufferSpan.Slice(0, _count);

    public void Add(byte value)
    {
        var buffer = BufferSpan;
        int count = Count;
        if ((uint)count < (uint)buffer.Length)
        {
            Count = count + 1;
            buffer[count] = value;
        }
        else
        {
            AddSlow(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddSlow(byte value)
    {
        EnsureCapacity(Count + 1);
        BufferSpan[Count++] = value;
    }

    public void AddRange(ReadOnlySpan<byte> span)
    {
        var buffer = GetAppendSpan(span.Length);
        span.CopyTo(buffer);
        Count += span.Length;
    }

    /// <summary>
    /// Get a span into the buffer that starts at the end of the current
    /// buffer and is large enough to hold the specified number of bytes.
    /// The <see cref="Count"/> is not updated.
    /// </summary>
    public Span<byte> GetAppendSpan(int size)
    {
        var count = Count;
        EnsureCapacity(count + size);
        return BufferSpan.Slice(count, size);
    }

    public void EnsureCapacity(int capacity)
    {
        Debug.Assert(capacity >= 0);

        if (BufferSpan.Length < capacity)
        {
            Grow(GetNewCapacity(capacity));
        }
    }

    public void Clear()
    {
        _count = 0;
    }

    private const int DefaultCapacity = 64;

    private int GetNewCapacity(int capacity)
    {
        var buffer = BufferSpan;
        Debug.Assert(buffer.Length < capacity);

        int newCapacity = buffer.Length == 0 ? DefaultCapacity : buffer.Length * 2;

        if ((uint)newCapacity < (uint)capacity)
        {
            newCapacity = capacity;
        }
        return newCapacity;
    }

    private void Grow(int newSize)
    {
        var newArray = ArrayPool<byte>.Shared.Rent(newSize);
        BufferSpan.CopyTo(newArray);
        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
        }
        _rented = newArray;
    }

    public void Dispose()
    {
        Clear();
        if (_rented is not null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
            _rented = null;
        }
    }
}