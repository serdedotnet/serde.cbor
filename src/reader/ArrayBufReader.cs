
namespace Serde.IO;

internal struct ArrayBufReader(byte[] bytes) : IBufReader
{
    private readonly byte[] _buffer = bytes;
    private int _offset;

    public ReadOnlySpan<byte> Span => _buffer.AsSpan(_offset);

    public void Advance(int count)
    {
        _offset += count;
    }

    public bool FillBuffer(int fillCount)
    {
        return _offset + fillCount <= _buffer.Length;
    }
}