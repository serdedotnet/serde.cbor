using Serde;
using Serde.IO;

namespace Serde.Cbor;

public static partial class CborSerializer
{
    public static byte[] Serialize<T>(T value)
        where T : ISerializeProvider<T> => Serialize(value, T.Instance);

    public static byte[] Serialize<T>(T value, ISerialize<T> proxy) => ToArray(value, proxy);

    private readonly struct SpecializedBuffer(ScratchBuffer _buffer) : IByteWriter
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

    public static byte[] ToArray<T>(T value, ISerialize<T> proxy)
    {
        using var buffer = new ScratchBuffer(512);
        var writer = new CborWriter<SpecializedBuffer>(new(buffer));
        proxy.Serialize(value, writer);
        return buffer.WrittenMemory.ToArray();
    }

    public static void Serialize<T>(List<byte> list, T value, ISerialize<T> proxy)
    {
        var writer = new CborWriter<ListWriter>(new ListWriter(list));
        proxy.Serialize(value, writer);
    }

    public static T Deserialize<T, U>(byte[] bytes, U proxy)
        where U : IDeserialize<T>
    {
        var byteBuffer = new ArrayBufReader(bytes);
        using var reader = new CborReader<ArrayBufReader>(byteBuffer);
        return proxy.Deserialize(reader);
    }

    public static T Deserialize<T>(byte[] bytes)
        where T : IDeserializeProvider<T>
    {
        var byteBuffer = new ArrayBufReader(bytes);
        using var reader = new CborReader<ArrayBufReader>(byteBuffer);
        return T.Instance.Deserialize(reader);
    }
}
