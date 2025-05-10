
namespace Serde.Cbor.Tests;

/// <summary>
/// Compares the output of the MsgPackSerializer with the output of the MessagePackSerializer.
/// </summary>
public partial class SerializeOracleTests
{
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

    //[Fact]
    //public void TestNullableString()
    //{
    //    AssertCborEqual((string?)null, NullableRefProxy.Ser<string, StringProxy>.Instance);
    //}

    //[GenerateSerialize]
    //private enum ByteEnum : byte
    //{
    //    A, B, C
    //}

    //[Fact]
    //public void TestByteEnum()
    //{
    //    AssertCborEqual(ByteEnum.A, ByteEnumProxy.Instance);
    //    AssertCborEqual(ByteEnum.B, ByteEnumProxy.Instance);
    //    AssertCborEqual(ByteEnum.C, ByteEnumProxy.Instance);
    //}

    //[GenerateSerialize]
    //private enum IntEnum : int
    //{
    //    A, B, C
    //}

    //[Fact]
    //public void TestIntEnum()
    //{
    //    AssertCborEqual(IntEnum.A, IntEnumProxy.Instance);
    //    AssertCborEqual(IntEnum.B, IntEnumProxy.Instance);
    //    AssertCborEqual(IntEnum.C, IntEnumProxy.Instance);
    //}

    //[GenerateSerialize]
    //[SerdeTypeOptions(MemberFormat = MemberFormat.None)]
    //public partial record Point
    //{
    //    public int X { get; init; }
    //    public int Y { get; init; }
    //}

    //[Fact]
    //public void TestRecord()
    //{
    //    AssertCborEqual(new Point { X = 1, Y = 2 });
    //}

    //[Fact]
    //public void TestDouble()
    //{
    //    AssertCborEqual(3.14, F64Proxy.Instance);
    //    AssertCborEqual(double.NaN, F64Proxy.Instance);
    //    AssertCborEqual(double.PositiveInfinity, F64Proxy.Instance);
    //}

    //[Fact]
    //public void TestArray()
    //{
    //    AssertCborEqual(new[] { 1, 2, 3 }, ArrayProxy.Ser<int, I32Proxy>.Instance);
    //    AssertCborEqual(new[] { "a", "b", "c" }, ArrayProxy.Ser<string, StringProxy>.Instance);
    //    AssertCborEqual(new[] { new Point { X = 1, Y = 2 }, new Point { X = 3, Y = 4 } },
    //        ArrayProxy.Ser<Point, Point>.Instance);
    //}

    //[Fact]
    //public void TestDictionary()
    //{
    //    AssertCborEqual(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } },
    //        DictProxy.Ser<string, int, StringProxy, I32Proxy>.Instance);
    //    AssertCborEqual(new Dictionary<int, string> { { 1, "a" }, { 2, "b" } },
    //        DictProxy.Ser<int, string, I32Proxy, StringProxy>.Instance);
    //    AssertCborEqual(new Dictionary<Point, string> { { new Point { X = 1, Y = 2 }, "a" }, { new Point { X = 3, Y = 4 }, "b" } },
    //        DictProxy.Ser<Point, string, Point, StringProxy>.Instance);
    //}

    //[Fact]
    //public void TestFloat()
    //{
    //    AssertCborEqual(3.14f, F32Proxy.Instance);
    //    AssertCborEqual(float.NaN, F32Proxy.Instance);
    //    AssertCborEqual(float.PositiveInfinity, F32Proxy.Instance);
    //}

    private void AssertCborEqual<T, U>(T value, U proxy, byte[] expected)
        where U : ISerialize<T>
    {
        var actual = CborSerializer.Serialize(value, proxy);
        Assert.Equal(expected, actual);
    }

    private void AssertCborEqual<T>(T value, byte[] expected) where T : ISerializeProvider<T>
        => AssertCborEqual(value, T.Instance, expected);

}
