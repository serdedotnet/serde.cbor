using Serde;
using Serde.IO;

namespace Serde.Cbor;

public static class CborSerializer
{
    public static byte[] Serialize<T>(T value)
        where T : ISerializeProvider<T>
        => Serialize(value, T.Instance);

    public static byte[] Serialize<T>(T value, ISerialize<T> proxy)
    {
        using var buffer = new ScratchBuffer(1024);
        var writer = new CborWriter(buffer);
        proxy.Serialize(value, writer);
        return buffer.Span.ToArray();
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
