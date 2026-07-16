using System.Text;

namespace Serde.Cbor;

internal readonly struct ListWriter : IByteWriter
{
    private static readonly Encoding s_utf8 = new UTF8Encoding(false, true);
    private readonly List<byte> _list;

    public ListWriter()
    {
        _list = new List<byte>();
    }

    public ListWriter(List<byte> list)
    {
        _list = list;
    }

    public void WriteByte(byte b)
    {
        _list.Add(b);
    }

    public void WriteBytes(ReadOnlyMemory<byte> bytes)
    {
        _list.AddRange(bytes.Span);
    }

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _list.AddRange(bytes);
    }

    public void WriteString(string value, int maxByteCount = -1)
    {
        if (maxByteCount < 0)
        {
            maxByteCount = s_utf8.GetMaxByteCount(value.Length);
        }
        _list.AddRange(s_utf8.GetBytes(value));
    }
}
