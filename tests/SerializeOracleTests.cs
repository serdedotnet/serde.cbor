
namespace Serde.Cbor.Tests;

/// <summary>
/// Compares the output of the MsgPackSerializer with the output of the MessagePackSerializer.
/// </summary>
public partial class SerializeOracleTests
{
    [Fact]
    public void TestBool()
    {
        AssertCborEqual(true, BoolProxy.Instance, [ 0xf5 ]);
        AssertCborEqual(false, BoolProxy.Instance, [ 0xf4 ]);
    }

    //[Fact]
    //public void TestByte()
    //{
    //    AssertCborEqual((byte)42, U8Proxy.Instance);
    //    AssertCborEqual((byte)0xf0, U8Proxy.Instance);
    //}

    //[Fact]
    //public void TestChar()
    //{
    //    AssertCborEqual('c', CharProxy.Instance);
    //}

    //[Fact]
    //public void TestByteSizedUInt()
    //{
    //    AssertCborEqual(42u, U32Proxy.Instance);
    //}

    //[Fact]
    //public void TestPositiveByteSizedInt()
    //{
    //    AssertCborEqual(42, I32Proxy.Instance);
    //}

    //[Fact]
    //public void TestNegativeByteSizedInt()
    //{
    //    AssertCborEqual(-42, I32Proxy.Instance);
    //}

    //[Fact]
    //public void TestPositiveUInt16()
    //{
    //    AssertCborEqual((ushort)0x1000, U16Proxy.Instance);
    //}

    //[Fact]
    //public void TestNegativeInt16()
    //{
    //    AssertCborEqual((short)-0x1000, I16Proxy.Instance);
    //}

    [Fact]
    public void TestString()
    {
        AssertCborEqual("hello", StringProxy.Instance, [
            0x65, 0x68, 0x65, 0x6c, 0x6c, 0x6f
        ]);
    }

    [Fact]
    public void TestNullableString()
    {
        AssertCborEqual((string?)null,
            NullableRefProxy.Ser<string, StringProxy>.Instance,
            [ 0xf6 ]
        );
    }

    [GenerateSerialize]
    private enum ByteEnum : byte
    {
        A, B, C
    }

    [Fact]
    public void TestByteEnum()
    {
        AssertCborEqual(ByteEnum.A, ByteEnumProxy.Instance, [ 0x00 ]);
        AssertCborEqual(ByteEnum.B, ByteEnumProxy.Instance, [ 0x01 ]);
        AssertCborEqual(ByteEnum.C, ByteEnumProxy.Instance, [ 0x02 ]);
    }

    [GenerateSerialize]
    private enum IntEnum : int
    {
        A, B, C
    }

    [Fact]
    public void TestIntEnum()
    {
        AssertCborEqual(IntEnum.A, IntEnumProxy.Instance, [ 0x00 ]);
        AssertCborEqual(IntEnum.B, IntEnumProxy.Instance, [ 0x01 ]);
        AssertCborEqual(IntEnum.C, IntEnumProxy.Instance, [ 0x02 ]);
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
        AssertCborEqual(new Point { X = 1, Y = 2 }, [
            0xa2, 0x61, 0x58, 0x01, 0x61, 0x59, 0x02
        ]);
    }

    [Fact]
    public void TestDouble()
    {
        AssertCborEqual(3.14, F64Proxy.Instance, [
            0xfb, 0x40, 0x09, 0x1e, 0xb8, 0x51, 0xeb, 0x85, 0x1f
        ]);
        AssertCborEqual(double.NaN, F64Proxy.Instance, [
            0xfb, 0xff, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ]);
        AssertCborEqual(double.PositiveInfinity, F64Proxy.Instance, [
            0xfb, 0x7f, 0xf0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ]);
    }

    [Fact]
    public void TestArray()
    {
        AssertCborEqual(new[] { 1, 2, 3 }, ArrayProxy.Ser<int, I32Proxy>.Instance,
            [ 0x83, 0x01, 0x02, 0x03 ]
        );
        AssertCborEqual(new[] { "a", "b", "c" }, ArrayProxy.Ser<string, StringProxy>.Instance,
            [ 0x83, 0x61, 0x61, 0x61, 0x62, 0x61, 0x63 ]
        );
        AssertCborEqual(new[] { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } },
            ArrayProxy.Ser<Point, Point>.Instance,
            [ 0x82,
                0xa2, 0x61, 0x58, 0x01, 0x61, 0x59, 0x02,
                0xa2, 0x61, 0x58, 0x03, 0x61, 0x59, 0x04,
            ]
        );
    }

    [Fact]
    public void TestDictionary()
    {
        AssertCborEqual(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
            DictProxy.Ser<string, int, StringProxy, I32Proxy>.Instance,
            [ 0xa2, 0x61, 0x61, 0x01, 0x61, 0x62, 0x02 ]
        );
        AssertCborEqual(new Dictionary<int, string> { { 1, "a" }, { 2, "b" } },
            DictProxy.Ser<int, string, I32Proxy, StringProxy>.Instance,
            [ 0xa2, 0x01, 0x61, 0x61, 0x02, 0x61, 0x62 ]
        );
        AssertCborEqual(
            new Dictionary<Point, string> {
                [new Point { X = 1, Y = 2 }] = "a",
                [new Point { X = 3, Y = 4 }] = "b"
            },
            DictProxy.Ser<Point, string, Point, StringProxy>.Instance,
            [ 0xa2,
                0xa2, 0x61, 0x58, 0x01, 0x61, 0x59, 0x02,
                0x61, 0x61,
                0xa2, 0x61, 0x58, 0x03, 0x61, 0x59, 0x04,
                0x61, 0x62
            ]
        );
    }

    [Fact]
    public void TestFloat()
    {
        AssertCborEqual(3.14f, F32Proxy.Instance,
            [0xfa, 0x40, 0x48, 0xf5, 0xc3]
        );
        AssertCborEqual(float.NaN, F32Proxy.Instance,
            [0xfa, 0xff, 0xc0, 0x00, 0x00]
        );
        AssertCborEqual(float.PositiveInfinity, F32Proxy.Instance,
            [0xfa, 0x7f, 0x80, 0x00, 0x00]
        );
    }

    private void AssertCborEqual<T, U>(T value, U proxy, byte[] expected)
        where U : ISerialize<T>
    {
        var actual = CborSerializer.Serialize(value, proxy);
        Assert.Equal(expected, actual);
    }

    private void AssertCborEqual<T>(T value, byte[] expected) where T : ISerializeProvider<T>
        => AssertCborEqual(value, T.Instance, expected);

}
