using System.Buffers;
using Serde;
using Serde.IO;

namespace Serde.Cbor;

public static partial class CborSerializer
{
    public static byte[] Serialize<T>(T value)
        where T : ISerializeProvider<T> => Serialize(value, T.Instance);

    public static byte[] Serialize<T>(T value, ISerialize<T> proxy) => ToArray(value, proxy);

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

    public static void Serialize<T, TWriter>(TWriter writer, T value, ISerialize<T> proxy)
        where TWriter : IBufferWriter<byte>
    {
        var cborWriter = new CborWriter<BufWriterAdapter<TWriter>>(new(writer));
        proxy.Serialize(value, cborWriter);
    }

    public static T Deserialize<T>(byte[] bytes)
        where T : IDeserializeProvider<T> => Deserialize(bytes.AsMemory(), T.Instance);

    public static T Deserialize<T, U>(byte[] bytes, IDeserialize<T> proxy)
        => Deserialize(bytes.AsMemory(), proxy);

    public static T Deserialize<T>(Memory<byte> bytes, IDeserialize<T> proxy)
    {
        var byteBuffer = new ArrayBufReader(bytes);
        using var reader = new CborReader<ArrayBufReader>(byteBuffer);
        return proxy.Deserialize(reader);
    }
}
