
using System.Buffers;
using System.Text;

namespace Serde.Cbor;

internal struct BufWriterAdapter<TWriter>(TWriter _writer) : IByteWriter
    where TWriter : IBufferWriter<byte>
{
    private static readonly Encoding s_utf8 = new UTF8Encoding(false, true);

    public void WriteByte(byte b)
    {
        _writer.Write([ b ]);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        _writer.Write(bytes.Span);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _writer.Write(bytes);
    }

    public void WriteString(string value, int maxByteCount = -1)
    {
        if (maxByteCount < 0)
        {
            maxByteCount = s_utf8.GetMaxByteCount(value.Length);
        }
        var span = _writer.GetSpan(maxByteCount);
        var bytesWritten = s_utf8.GetBytes(value, span);
        _writer.Advance(bytesWritten);
    }
}