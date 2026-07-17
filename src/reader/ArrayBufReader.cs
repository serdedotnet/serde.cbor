namespace Serde.IO;

internal struct ArrayBufReader(Memory<byte> bytes) : IBufReader
{
    private readonly Memory<byte> _buffer = bytes;
    private int _offset;

    public ReadOnlySpan<byte> Span => _buffer.Span.Slice(_offset);

    public void Advance(int count)
    {
        _offset += count;
    }

    public bool FillBuffer(int fillCount)
    {
        return _offset + fillCount <= _buffer.Length;
    }
}
