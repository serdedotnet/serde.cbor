
namespace Serde.Cbor.Tests;

public partial class RoundTripTests
{
    [Fact]
    public void TestBool()
    {
        AssertRoundTrip(true, BoolProxy.Instance);
        AssertRoundTrip(false, BoolProxy.Instance);
    }

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
        AssertRoundTrip(-1, I32Proxy.Instance);
        AssertRoundTrip(-24, I32Proxy.Instance);
        AssertRoundTrip(-25, I32Proxy.Instance);
        AssertRoundTrip(-42, I32Proxy.Instance);
        AssertRoundTrip(-128, I32Proxy.Instance);
        AssertRoundTrip(-129, I32Proxy.Instance);
        AssertRoundTrip(-256, I32Proxy.Instance);
        AssertRoundTrip(-257, I32Proxy.Instance);
    }

    [Fact]
    public void TestI64Boundaries()
    {
        AssertRoundTrip(long.MinValue, I64Proxy.Instance);
        AssertRoundTrip(long.MaxValue, I64Proxy.Instance);
        AssertRoundTrip((long)int.MaxValue + 1, I64Proxy.Instance);
        AssertRoundTrip((long)int.MinValue - 1, I64Proxy.Instance);
    }

    [Fact]
    public void TestU64Boundaries()
    {
        AssertRoundTrip(ulong.MaxValue, U64Proxy.Instance);
        AssertRoundTrip((ulong)uint.MaxValue + 1, U64Proxy.Instance);
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
        var proxy = new ByteEnumProxy();
        AssertRoundTrip(ByteEnum.A, proxy);
        AssertRoundTrip(ByteEnum.B, proxy);
        AssertRoundTrip(ByteEnum.C, proxy);
    }

    [GenerateSerde]
    private enum IntEnum : int
    {
        A, B, C
    }

    [Fact]
    public void TestIntEnum()
    {
        var proxy = new IntEnumProxy();
        AssertRoundTrip(IntEnum.A, proxy);
        AssertRoundTrip(IntEnum.B, proxy);
        AssertRoundTrip(IntEnum.C, proxy);
    }

    // Name-based encoding must round-trip regardless of the underlying values,
    // since the wire form is the variant name rather than the numeric value.
    [GenerateSerde]
    private enum SparseEnum : byte
    {
        A = 2,
        B = 3,
        C = 255,
    }

    [Fact]
    public void TestSparseEnum()
    {
        var proxy = new SparseEnumProxy();
        AssertRoundTrip(SparseEnum.A, proxy);
        AssertRoundTrip(SparseEnum.B, proxy);
        AssertRoundTrip(SparseEnum.C, proxy);
    }

    // AsUnderlying enums round-trip through the numeric primitive path.
    [GenerateSerde(AsUnderlying = true)]
    private enum AsByteEnum : byte
    {
        A = 1,
        B = 2,
        C = 3,
    }

    [Fact]
    public void TestAsByteEnum()
    {
        var proxy = new AsByteEnumProxy();
        AssertRoundTrip(AsByteEnum.A, proxy);
        AssertRoundTrip(AsByteEnum.B, proxy);
        AssertRoundTrip(AsByteEnum.C, proxy);
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
    public void TestArrayWith16Elements()
    {
        // Exercises arrays with >15 elements (multi-byte length path 0x98)
        var arr = Enumerable.Range(0, 16).ToArray();
        AssertRoundTrip<
            int[],
            ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>(arr);
    }

    [Fact]
    public void TestArrayWith25Elements()
    {
        // Exercises arrays with >23 elements (multi-byte length path 0x98)
        var arr = Enumerable.Range(0, 25).ToArray();
        AssertRoundTrip<
            int[],
            ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>(arr);
    }

    [Fact]
    public void TestArrayWith300Elements()
    {
        // Exercises 16-bit array length path (0x99)
        var arr = Enumerable.Range(0, 300).ToArray();
        AssertRoundTrip<
            int[],
            ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>(arr);
    }

    [Fact]
    public void TestU32LargeValues()
    {
        // Exercises the 4-byte unsigned integer path
        AssertRoundTrip(100000u, U32Proxy.Instance);
        AssertRoundTrip(uint.MaxValue, U32Proxy.Instance);
    }

    [Fact]
    public void TestU64LargeValues()
    {
        // Exercises the 8-byte unsigned integer path
        AssertRoundTrip((ulong)uint.MaxValue + 1, U64Proxy.Instance);
        AssertRoundTrip(ulong.MaxValue, U64Proxy.Instance);
    }

    [Fact]
    public void TestNegativeI64Values()
    {
        // Exercises the 8-byte negative integer path
        AssertRoundTrip(long.MinValue, I64Proxy.Instance);
        AssertRoundTrip((long)int.MinValue - 1, I64Proxy.Instance);
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

    [GenerateSerde]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    public partial record WithNullable : IEquatable<WithNullable>
    {
        public int X { get; init; }
        public string? Name { get; init; }
    }

    // A null nullable member is skipped by the serializer (WriteStringIfNotNull), so the
    // map header must reflect the actual pair count, not the declared field count.
    [Fact]
    public void TestNullableMemberSkipped()
    {
        AssertRoundTrip(new WithNullable { X = 1, Name = null });
        AssertRoundTrip(new WithNullable { X = 1, Name = "hi" });
    }

    [GenerateSerde]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    public partial record Nested : IEquatable<Nested>
    {
        public WithNullable Inner { get; init; } = null!;
        public string? Tag { get; init; }
    }

    // Nested custom types each maintain their own backpatched header frame.
    [Fact]
    public void TestNestedNullableMemberSkipped()
    {
        AssertRoundTrip(new Nested { Inner = new WithNullable { X = 5, Name = null }, Tag = null });
        AssertRoundTrip(new Nested { Inner = new WithNullable { X = 5, Name = "a" }, Tag = "b" });
    }

    [GenerateSerde]
    private abstract partial record Shape
    {
        private Shape() { }

        public sealed record Circle(double Radius) : Shape;
        public sealed record Rectangle(double Width, double Height) : Shape;
        // A case with no fields exercises the empty nested-map path.
        public sealed record Unit : Shape;
    }

    // Each union case must round-trip: the deserializer selects the variant by the
    // case name key and reconstructs the nested map of that case's fields.
    [Fact]
    public void TestUnionCircle()
    {
        AssertRoundTrip<Shape>(new Shape.Circle(2.5));
    }

    [Fact]
    public void TestUnionRectangle()
    {
        AssertRoundTrip<Shape>(new Shape.Rectangle(3.0, 4.0));
    }

    [Fact]
    public void TestUnionFieldlessCase()
    {
        AssertRoundTrip<Shape>(new Shape.Unit());
    }

    // A union nested inside a custom type must round-trip through its own map frame.
    [GenerateSerde]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    private partial record HasShape : IEquatable<HasShape>
    {
        public Shape Shape { get; init; } = null!;
        public string Label { get; init; } = "";
    }

    [Fact]
    public void TestUnionNestedInRecord()
    {
        AssertRoundTrip(new HasShape { Shape = new Shape.Circle(1.0), Label = "c" });
        AssertRoundTrip(new HasShape { Shape = new Shape.Rectangle(1.0, 2.0), Label = "r" });
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
