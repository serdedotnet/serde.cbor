
namespace Serde.Cbor.Tests;

public partial class RoundTripTests
{
    [Fact]
    public void TestChar()
    {
        AssertRoundTrip('c', CharProxy.Instance);
    }

    [Fact]
    public void TestByte()
    {
        AssertRoundTrip((byte)42, U8Proxy.Instance);
        AssertRoundTrip((byte)0xf0, U8Proxy.Instance);
    }

    [Fact]
    public void TestByteSizedUInt()
    {
        AssertRoundTrip(42u, U32Proxy.Instance);
    }

    [Fact]
    public void TestPositiveByteSizedInt()
    {
        AssertRoundTrip(42, I32Proxy.Instance);
    }

    [Fact]
    public void TestNegativeByteSizedInt()
    {
        AssertRoundTrip(-42, I32Proxy.Instance);
    }

    [Fact]
    public void TestPositiveUInt16()
    {
        AssertRoundTrip((ushort)0x1000, U16Proxy.Instance);
    }

    [Fact]
    public void TestNegativeInt16()
    {
        AssertRoundTrip((short)-0x1000, I16Proxy.Instance);
    }

    [Fact]
    public void TestString()
    {
        AssertRoundTrip("hello", StringProxy.Instance);
    }

    [Fact]
    public void TestLongerString()
    {
        AssertRoundTrip(new string('A', 100), StringProxy.Instance);
    }

    [Fact]
    public void TestNullableString()
    {
        AssertRoundTrip<
            string?,
            NullableRefProxy.Ser<string, StringProxy>,
            NullableRefProxy.De<string, StringProxy>>(null);
    }

    [GenerateSerde]
    private enum ByteEnum : byte
    {
        A, B, C
    }

    [Fact]
    public void TestByteEnum()
    {
        AssertRoundTrip(ByteEnum.A, ByteEnumProxy.Instance);
        AssertRoundTrip(ByteEnum.B, ByteEnumProxy.Instance);
        AssertRoundTrip(ByteEnum.C, ByteEnumProxy.Instance);
    }

    [GenerateSerde]
    private enum IntEnum : int
    {
        A, B, C
    }

    [Fact]
    public void TestIntEnum()
    {
        AssertRoundTrip(IntEnum.A, IntEnumProxy.Instance);
        AssertRoundTrip(IntEnum.B, IntEnumProxy.Instance);
        AssertRoundTrip(IntEnum.C, IntEnumProxy.Instance);
    }

    [GenerateSerde]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    public partial record Point
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    [Fact]
    public void TestRecord()
    {
        AssertRoundTrip(new Point { X = 1, Y = 2 });
    }

    [Fact]
    public void TestDouble()
    {
        AssertRoundTrip(3.14, F64Proxy.Instance);
        AssertRoundTrip(double.NaN, F64Proxy.Instance);
        AssertRoundTrip(double.PositiveInfinity, F64Proxy.Instance);
    }

    [Fact]
    public void TestArray()
    {
        AssertRoundTrip<
            int[],
            ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>(new[] { 1, 2, 3 });
        AssertRoundTrip<
            string[],
            ArrayProxy.Ser<string, StringProxy>,
            ArrayProxy.De<string, StringProxy>>(new[] { "a", "b", "c" });
        AssertRoundTrip<
            Point[],
            ArrayProxy.Ser<Point, Point>,
            ArrayProxy.De<Point, Point>>(
                new[] { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } });
    }

    [Fact]
    public void TestDictionary()
    {
        AssertRoundTrip<
            Dictionary<string, int>,
            DictProxy.Ser<string, int, StringProxy, I32Proxy>,
            DictProxy.De<string, int, StringProxy, I32Proxy>>(
                new Dictionary<string, int> { { "a", 1 }, { "b", 2 } });
        AssertRoundTrip<
            Dictionary<int, string>,
            DictProxy.Ser<int, string, I32Proxy, StringProxy>,
            DictProxy.De<int, string, I32Proxy, StringProxy>>(
                new Dictionary<int, string> { { 1, "a" }, { 2, "b" } });
        AssertRoundTrip<
            Dictionary<Point, string>,
            DictProxy.Ser<Point, string, Point, StringProxy>,
            DictProxy.De<Point, string, Point, StringProxy>>(
                new Dictionary<Point, string> { { new Point { X = 1, Y = 2 }, "a" }, { new Point { X = 3, Y = 4 }, "b" } });
    }

    [Fact]
    public void TestFloat()
    {
        AssertRoundTrip(3.14f, F32Proxy.Instance);
        AssertRoundTrip(float.NaN, F32Proxy.Instance);
        AssertRoundTrip(float.PositiveInfinity, F32Proxy.Instance);
    }

    [Fact]
    public void TestBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        AssertRoundTrip(bytes, ByteArrayProxy.Instance);
    }

    private static void AssertRoundTrip<T>(T expected)
        where T : ISerializeProvider<T>, IDeserializeProvider<T>, IEquatable<T>
    {
        AssertRoundTrip(expected, SerializeProvider.GetSerialize<T, T>(), DeserializeProvider.GetDeserialize<T, T>());
    }

    private static void AssertRoundTrip<T, TSerialize>(T expected, TSerialize serializeObject)
        where TSerialize : ISerialize<T>, IDeserializeProvider<T>
    {
        AssertRoundTrip(expected, serializeObject, TSerialize.Instance);
    }

    private static void AssertRoundTrip<T, TSerialize, TDeserialize>(T expected)
        where TSerialize : ISerializeProvider<T>
        where TDeserialize : IDeserializeProvider<T>
    {
        var serialized = CborSerializer.Serialize(expected, TSerialize.Instance);
        var actual = CborSerializer.Deserialize<T, IDeserialize<T>>(serialized, TDeserialize.Instance);
        Assert.Equal(expected, actual);
    }

    private static void AssertRoundTrip<T, TSerialize, TDeserialize>(T expected, TSerialize serialize, TDeserialize deserialize)
        where TSerialize : ISerialize<T>
        where TDeserialize : IDeserialize<T>
    {
        var serialized = CborSerializer.Serialize(expected, serialize);
        var actual = CborSerializer.Deserialize<T, IDeserialize<T>>(serialized, deserialize);
        Assert.Equal(expected, actual);
    }
}
