namespace Serde.Cbor.Tests;

/// <summary>
/// Verifies that the CborSerializer produces correct CBOR byte sequences.
/// </summary>
public partial class SerializeOracleTests
{
    [Fact]
    public void TestBool()
    {
        AssertCborEqual(true, BoolProxy.Instance, [0xf5]);
        AssertCborEqual(false, BoolProxy.Instance, [0xf4]);
    }

    [Fact]
    public void TestByte()
    {
        AssertCborEqual((byte)0, U8Proxy.Instance, [0x00]);
        AssertCborEqual((byte)23, U8Proxy.Instance, [0x17]);
        AssertCborEqual((byte)24, U8Proxy.Instance, [0x18, 0x18]);
        AssertCborEqual((byte)42, U8Proxy.Instance, [0x18, 0x2a]);
        AssertCborEqual((byte)0xff, U8Proxy.Instance, [0x18, 0xff]);
    }

    [Fact]
    public void TestChar()
    {
        // char is encoded as a UTF-8 string
        AssertCborEqual('c', CharProxy.Instance, [0x61, 0x63]);
    }

    [Fact]
    public void TestByteSizedUInt()
    {
        AssertCborEqual(42u, U32Proxy.Instance, [0x18, 0x2a]);
    }

    [Fact]
    public void TestPositiveByteSizedInt()
    {
        AssertCborEqual(0, I32Proxy.Instance, [0x00]);
        AssertCborEqual(23, I32Proxy.Instance, [0x17]);
        AssertCborEqual(24, I32Proxy.Instance, [0x18, 0x18]);
        AssertCborEqual(42, I32Proxy.Instance, [0x18, 0x2a]);
        AssertCborEqual(255, I32Proxy.Instance, [0x18, 0xff]);
        AssertCborEqual(256, I32Proxy.Instance, [0x19, 0x01, 0x00]);
    }

    [Fact]
    public void TestNegativeByteSizedInt()
    {
        // CBOR negative: -1 is 0x20, -24 is 0x37, -25 is 0x38 0x18
        AssertCborEqual(-1, I32Proxy.Instance, [0x20]);
        AssertCborEqual(-24, I32Proxy.Instance, [0x37]);
        AssertCborEqual(-25, I32Proxy.Instance, [0x38, 0x18]);
        AssertCborEqual(-42, I32Proxy.Instance, [0x38, 0x29]);
        AssertCborEqual(-128, I32Proxy.Instance, [0x38, 0x7f]);
        AssertCborEqual(-129, I32Proxy.Instance, [0x38, 0x80]);
        AssertCborEqual(-256, I32Proxy.Instance, [0x38, 0xff]);
        AssertCborEqual(-257, I32Proxy.Instance, [0x39, 0x01, 0x00]);
    }

    [Fact]
    public void TestPositiveUInt16()
    {
        AssertCborEqual((ushort)0x1000, U16Proxy.Instance, [0x19, 0x10, 0x00]);
    }

    [Fact]
    public void TestNegativeInt16()
    {
        // -0x1000 = -4096 = -1 - 4095, wire value 4095 = 0x0FFF
        AssertCborEqual((short)-0x1000, I16Proxy.Instance, [0x39, 0x0f, 0xff]);
    }

    [Fact]
    public void TestI64Boundaries()
    {
        AssertCborEqual(
            long.MinValue,
            I64Proxy.Instance,
            [0x3b, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]
        );
    }

    [Fact]
    public void TestString()
    {
        AssertCborEqual("hello", StringProxy.Instance, [0x65, 0x68, 0x65, 0x6c, 0x6c, 0x6f]);
    }

    [Fact]
    public void TestNullableString()
    {
        AssertCborEqual((string?)null, NullableRefProxy.Ser<string, StringProxy>.Instance, [0xf6]);
    }

    [GenerateSerialize]
    private enum ByteEnum : byte
    {
        A,
        B,
        C,
    }

    [Fact]
    public void TestByteEnum()
    {
        // Enums are serialized as a CBOR text string of the variant name, emitted
        // using serde's default member format (camelCase): A -> "a", etc.
        var proxy = new ByteEnumProxy();
        AssertCborEqual(ByteEnum.A, proxy, [0x61, (byte)'a']);
        AssertCborEqual(ByteEnum.B, proxy, [0x61, (byte)'b']);
        AssertCborEqual(ByteEnum.C, proxy, [0x61, (byte)'c']);
    }

    [GenerateSerialize]
    private enum IntEnum : int
    {
        A,
        B,
        C,
    }

    [Fact]
    public void TestIntEnum()
    {
        var proxy = new IntEnumProxy();
        AssertCborEqual(IntEnum.A, proxy, [0x61, (byte)'a']);
        AssertCborEqual(IntEnum.B, proxy, [0x61, (byte)'b']);
        AssertCborEqual(IntEnum.C, proxy, [0x61, (byte)'c']);
    }

    // With AsUnderlying = true, the enum is serialized as its underlying integral value
    // via the normal primitive path rather than as a name string.
    [GenerateSerialize(AsUnderlying = true)]
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
        AssertCborEqual(AsByteEnum.A, proxy, [0x01]);
        AssertCborEqual(AsByteEnum.B, proxy, [0x02]);
        AssertCborEqual(AsByteEnum.C, proxy, [0x03]);
    }

    [GenerateSerialize]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    public partial record Point
    {
        public int X { get; init; }
        public int Y { get; init; }
    }

    [Fact]
    public void TestRecord()
    {
        AssertCborEqual(new Point { X = 1, Y = 2 }, [0xa2, 0x61, 0x58, 0x01, 0x61, 0x59, 0x02]);
    }

    [GenerateSerialize]
    [SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    public partial record WithNullable
    {
        public int X { get; init; }
        public string? Name { get; init; }
    }

    // When a nullable member is null it is skipped, so the map header must encode one
    // pair (0xa1), not the declared field count of two.
    [Fact]
    public void TestSkippedNullMemberHeaderCount()
    {
        AssertCborEqual(new WithNullable { X = 1, Name = null }, [0xa1, 0x61, 0x58, 0x01]);
        AssertCborEqual(
            new WithNullable { X = 1, Name = "hi" },
            [0xa2, 0x61, 0x58, 0x01, 0x64, 0x4e, 0x61, 0x6d, 0x65, 0x62, 0x68, 0x69]
        );
    }

    [Fact]
    public void TestDouble()
    {
        AssertCborEqual(
            3.14,
            F64Proxy.Instance,
            [0xfb, 0x40, 0x09, 0x1e, 0xb8, 0x51, 0xeb, 0x85, 0x1f]
        );
        AssertCborEqual(
            double.NaN,
            F64Proxy.Instance,
            [0xfb, 0xff, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        );
        AssertCborEqual(
            double.PositiveInfinity,
            F64Proxy.Instance,
            [0xfb, 0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        );
    }

    [Fact]
    public void TestArray()
    {
        AssertCborEqual(
            new[] { 1, 2, 3 },
            ArrayProxy.Ser<int, I32Proxy>.Instance,
            [0x83, 0x01, 0x02, 0x03]
        );
        AssertCborEqual(
            new[] { "a", "b", "c" },
            ArrayProxy.Ser<string, StringProxy>.Instance,
            [0x83, 0x61, 0x61, 0x61, 0x62, 0x61, 0x63]
        );
        AssertCborEqual(
            new[]
            {
                new Point { X = 1, Y = 2 },
                new Point { X = 3, Y = 4 },
            },
            ArrayProxy.Ser<Point, Point>.Instance,
            [
                0x82,
                0xa2,
                0x61,
                0x58,
                0x01,
                0x61,
                0x59,
                0x02,
                0xa2,
                0x61,
                0x58,
                0x03,
                0x61,
                0x59,
                0x04,
            ]
        );
    }

    [Fact]
    public void TestDictionary()
    {
        AssertCborEqual(
            new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
            DictProxy.Ser<string, int, StringProxy, I32Proxy>.Instance,
            [0xa2, 0x61, 0x61, 0x01, 0x61, 0x62, 0x02]
        );
        AssertCborEqual(
            new Dictionary<int, string> { { 1, "a" }, { 2, "b" } },
            DictProxy.Ser<int, string, I32Proxy, StringProxy>.Instance,
            [0xa2, 0x01, 0x61, 0x61, 0x02, 0x61, 0x62]
        );
        AssertCborEqual(
            new Dictionary<Point, string>
            {
                [new Point { X = 1, Y = 2 }] = "a",
                [new Point { X = 3, Y = 4 }] = "b",
            },
            DictProxy.Ser<Point, string, Point, StringProxy>.Instance,
            [
                0xa2,
                0xa2,
                0x61,
                0x58,
                0x01,
                0x61,
                0x59,
                0x02,
                0x61,
                0x61,
                0xa2,
                0x61,
                0x58,
                0x03,
                0x61,
                0x59,
                0x04,
                0x61,
                0x62,
            ]
        );
    }

    [Fact]
    public void TestFloat()
    {
        AssertCborEqual(3.14f, F32Proxy.Instance, [0xfa, 0x40, 0x48, 0xf5, 0xc3]);
        AssertCborEqual(float.NaN, F32Proxy.Instance, [0xfa, 0xff, 0xc0, 0x00, 0x00]);
        AssertCborEqual(float.PositiveInfinity, F32Proxy.Instance, [0xfa, 0x7f, 0x80, 0x00, 0x00]);
    }

    [GenerateSerde]
    private abstract partial record TestUnion
    {
        private TestUnion() { }

        public sealed record A(int X) : TestUnion;

        public sealed record B(string Name) : TestUnion;
    }

    [Fact]
    public void TestUnionMethod()
    {
        var u = new TestUnion.A(42);
        // should be serialized as a map with the case as the key and the case's fields as a nested
        // map.
        AssertCborEqual<TestUnion>(u, [0xa1, 0x61, 0x41, 0xa1, 0x61, 0x78, 0x18, 0x2a]);
    }

    [Fact]
    public void TestUnionCaseB()
    {
        var u = new TestUnion.B("hi");
        // { "B": { "name": "hi" } }
        AssertCborEqual<TestUnion>(
            u,
            [0xa1, 0x61, 0x42, 0xa1, 0x64, 0x6e, 0x61, 0x6d, 0x65, 0x62, 0x68, 0x69]
        );
    }

    // Deserialization oracle: a fixed union encoding must decode to the right case.
    [Fact]
    public void TestUnionDeserializeA()
    {
        var actual = CborSerializer.Deserialize<TestUnion>([
            0xa1,
            0x61,
            0x41,
            0xa1,
            0x61,
            0x78,
            0x18,
            0x2a,
        ]);
        Assert.Equal(new TestUnion.A(42), actual);
    }

    [Fact]
    public void TestUnionDeserializeB()
    {
        var actual = CborSerializer.Deserialize<TestUnion>([
            0xa1,
            0x61,
            0x42,
            0xa1,
            0x64,
            0x6e,
            0x61,
            0x6d,
            0x65,
            0x62,
            0x68,
            0x69,
        ]);
        Assert.Equal(new TestUnion.B("hi"), actual);
    }

    private void AssertCborEqual<T, U>(T value, U proxy, byte[] expected)
        where U : ISerialize<T>
    {
        var actual = CborSerializer.Serialize(value, proxy);
        Assert.Equal(expected, actual);
    }

    private void AssertCborEqual<T>(T value, byte[] expected)
        where T : ISerializeProvider<T> => AssertCborEqual(value, T.Instance, expected);
}
