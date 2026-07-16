namespace Serde.Cbor;

internal interface IByteWriter
{
    void WriteByte(byte b);
    void WriteBytes(ReadOnlyMemory<byte> bytes);
    void WriteBytes(ReadOnlySpan<byte> bytes);
    void WriteString(string value, int maxByteCount = -1);
}
