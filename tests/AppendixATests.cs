
namespace Serde.Cbor.Tests;

/// <summary>
/// Conformance tests derived from RFC 8949 Appendix A test vectors.
/// See: https://www.rfc-editor.org/rfc/rfc8949.html#appendix-A
/// Source data: tests/testdata/appendix_a.json
///              https://github.com/cbor/test-vectors/blob/master/appendix_a.json
///
/// Vectors requiring features not yet supported are noted but excluded:
/// - Half-precision floats (CBOR additional info 25)
/// - Tagged values other than 0 and 1 (date/time)
/// - Simple values other than true/false/null (e.g., undefined)
/// - Indefinite-length encoding
/// - Bignum integers exceeding Int64/UInt64 range
/// </summary>
public partial class AppendixATests
{
    // ── Custom types for complex vectors ──────────────────────────

    /// <summary>
    /// Vector 68: {"a": 1, "b": [2, 3]}
    /// </summary>
    [GenerateSerde]
    private partial record MixedMap
    {
        public int A { get; init; }
        public required int[] B { get; init; }
    }

    /// <summary>
    /// Vector 70: {"a": "A", "b": "B", "c": "C", "d": "D", "e": "E"}
    /// </summary>
    [GenerateSerde]
    private partial record FiveStringMap
    {
        public required string A { get; init; }
        public required string B { get; init; }
        public required string C { get; init; }
        public required string D { get; init; }
        public required string E { get; init; }
    }

    // ── Unsigned Integers (Major Type 0) ──────────────────────────
    // Vectors 0–10: values from 0 to 2^64-1
    // Vector 11 (bignum 2^64) requires tagged encoding — skipped

    [Theory]
    [InlineData("00", 0UL)]
    [InlineData("01", 1UL)]
    [InlineData("0a", 10UL)]
    [InlineData("17", 23UL)]
    [InlineData("1818", 24UL)]
    [InlineData("1819", 25UL)]
    [InlineData("1864", 100UL)]
    [InlineData("1903e8", 1000UL)]
    [InlineData("1a000f4240", 1000000UL)]
    [InlineData("1b000000e8d4a51000", 1000000000000UL)]
    [InlineData("1bffffffffffffffff", 18446744073709551615UL)]
    public void UnsignedInteger(string hex, ulong expected)
    {
        AssertRoundTrip(hex, expected, U64Proxy.Instance);
    }

    // ── Negative Integers (Major Type 1) ──────────────────────────
    // Vectors 14–17: values from -1 to -1000
    // Skipped: vector 12 (-2^64 overflows Int64), vector 13 (bignum)

    [Theory]
    [InlineData("20", -1L)]
    [InlineData("29", -10L)]
    [InlineData("3863", -100L)]
    [InlineData("3903e7", -1000L)]
    public void NegativeInteger(string hex, long expected)
    {
        AssertRoundTrip(hex, expected, I64Proxy.Instance);
    }

    // ── Booleans (Major Type 7) ──────────────────────────────────
    // Vectors 40–41

    [Theory]
    [InlineData("f4", false)]
    [InlineData("f5", true)]
    public void Boolean(string hex, bool expected)
    {
        AssertRoundTrip(hex, expected, BoolProxy.Instance);
    }

    // ── Null (Major Type 7) ──────────────────────────────────────
    // Vector 42
    // Vector 43 (undefined) not supported — skipped
    // Vectors 44–46 (simple values 16, 24, 255) not supported — skipped

    [Fact]
    public void Null()
    {
        var bytes = Convert.FromHexString("f6");
        var actual = CborSerializer.Deserialize<string?, IDeserialize<string?>>(
            bytes, NullableRefProxy.De<string, StringProxy>.Instance);
        Assert.Null(actual);

        var serialized = CborSerializer.Serialize<string?>(
            null, NullableRefProxy.Ser<string, StringProxy>.Instance);
        Assert.Equal("F6", Convert.ToHexString(serialized));
    }

    // ── Text Strings (Major Type 3) ──────────────────────────────
    // Vectors 55–61

    [Theory]
    [InlineData("60", "")]
    [InlineData("6161", "a")]
    [InlineData("6449455446", "IETF")]
    [InlineData("62225c", "\"\\")]
    [InlineData("62c3bc", "\u00fc")]        // ü
    [InlineData("63e6b0b4", "\u6c34")]      // 水
    [InlineData("64f0908591", "\U00010151")] // 𐅑
    public void TextString(string hex, string expected)
    {
        AssertRoundTrip(hex, expected, StringProxy.Instance);
    }

    // ── Byte Strings (Major Type 2) ──────────────────────────────
    // Vectors 53–54
    // Vector 71 (indefinite-length) not supported — skipped

    [Fact]
    public void ByteString_Empty()
    {
        // Vector 53: h''
        AssertRoundTrip("40", Array.Empty<byte>(), ByteArrayProxy.Instance);
    }

    [Fact]
    public void ByteString_01020304()
    {
        // Vector 54: h'01020304'
        AssertRoundTrip("4401020304", new byte[] { 1, 2, 3, 4 }, ByteArrayProxy.Instance);
    }

    // ── Float64 (Major Type 7, additional info 27) ───────────────
    // Vectors 21, 26, 30 (double-precision, roundtrip=true)
    // Skipped: half-precision vectors 18–20, 22–23, 27–29

    [Fact]
    public void Float64()
    {
        // Vector 21: 1.1
        AssertRoundTrip("fb3ff199999999999a", 1.1, F64Proxy.Instance);
        // Vector 26: 1e+300
        AssertRoundTrip("fb7e37e43c8800759c", 1e300, F64Proxy.Instance);
        // Vector 30: -4.1
        AssertRoundTrip("fbc010666666666666", -4.1, F64Proxy.Instance);
    }

    // Vectors 37–39: double-precision Infinity/NaN/-Infinity
    // (roundtrip=false in RFC since canonical form is half-precision,
    //  but the library always encodes as f64)
    [Fact]
    public void Float64_SpecialValues()
    {
        // Vector 37: Infinity
        AssertDeserialize("fb7ff0000000000000", double.PositiveInfinity, F64Proxy.Instance);
        AssertSerialize(double.PositiveInfinity, "fb7ff0000000000000", F64Proxy.Instance);

        // Vector 39: -Infinity
        AssertDeserialize("fbfff0000000000000", double.NegativeInfinity, F64Proxy.Instance);
        AssertSerialize(double.NegativeInfinity, "fbfff0000000000000", F64Proxy.Instance);

        // Vector 38: NaN — .NET uses negative NaN (0xFFF8…) vs RFC positive NaN (0x7FF8…)
        var nanBytes = Convert.FromHexString("fb7ff8000000000000");
        var nanResult = CborSerializer.Deserialize<double, IDeserialize<double>>(
            nanBytes, F64Proxy.Instance);
        Assert.True(double.IsNaN(nanResult));
    }

    // ── Float32 (Major Type 7, additional info 26) ───────────────
    // Vectors 24–25 (single-precision, roundtrip=true)

    [Fact]
    public void Float32()
    {
        // Vector 24: 100000.0
        AssertRoundTrip("fa47c35000", 100000.0f, F32Proxy.Instance);
        // Vector 25: 3.4028235e+38 (float.MaxValue)
        AssertRoundTrip("fa7f7fffff", float.MaxValue, F32Proxy.Instance);
    }

    // Vectors 34–36: single-precision Infinity/NaN/-Infinity (roundtrip=false)
    [Fact]
    public void Float32_SpecialValues()
    {
        // Vector 34: Infinity
        AssertDeserialize("fa7f800000", float.PositiveInfinity, F32Proxy.Instance);
        AssertSerialize(float.PositiveInfinity, "fa7f800000", F32Proxy.Instance);

        // Vector 36: -Infinity
        AssertDeserialize("faff800000", float.NegativeInfinity, F32Proxy.Instance);
        AssertSerialize(float.NegativeInfinity, "faff800000", F32Proxy.Instance);

        // Vector 35: NaN — sign bit may differ between implementations
        var nanBytes = Convert.FromHexString("fa7fc00000");
        var nanResult = CborSerializer.Deserialize<float, IDeserialize<float>>(
            nanBytes, F32Proxy.Instance);
        Assert.True(float.IsNaN(nanResult));
    }

    // ── Arrays (Major Type 4) ────────────────────────────────────
    // Vectors 62–63, 65
    // Vector 64 ([1,[2,3],[4,5]]) requires heterogeneous list — skipped
    // Vectors 73–78 (indefinite-length) not supported — skipped

    [Fact]
    public void Array_Empty()
    {
        // Vector 62: []
        AssertCollectionRoundTrip<int[], ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>("80", Array.Empty<int>());
    }

    [Fact]
    public void Array_1_2_3()
    {
        // Vector 63: [1, 2, 3]
        AssertCollectionRoundTrip<int[], ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>("83010203", new[] { 1, 2, 3 });
    }

    [Fact]
    public void Array_1_to_25()
    {
        // Vector 65: [1, 2, 3, ..., 25]
        AssertCollectionRoundTrip<int[], ArrayProxy.Ser<int, I32Proxy>,
            ArrayProxy.De<int, I32Proxy>>(
            "98190102030405060708090a0b0c0d0e0f101112131415161718181819",
            Enumerable.Range(1, 25).ToArray());
    }

    // ── Maps (Major Type 5) ──────────────────────────────────────
    // Vectors 66–68, 70
    // Vector 69 (["a",{"b":"c"}]) is a heterogeneous list — skipped
    // Vectors 79–81 (indefinite-length) not supported — skipped

    [Fact]
    public void Map_Empty()
    {
        // Vector 66: {}
        AssertCollectionRoundTrip<
            Dictionary<string, string>,
            DictProxy.Ser<string, string, StringProxy, StringProxy>,
            DictProxy.De<string, string, StringProxy, StringProxy>>(
            "a0", new Dictionary<string, string>());
    }

    [Fact]
    public void Map_IntToInt()
    {
        // Vector 67: {1: 2, 3: 4}
        var hex = "a201020304";
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<
            Dictionary<int, int>,
            IDeserialize<Dictionary<int, int>>>(
            bytes, DictProxy.De<int, int, I32Proxy, I32Proxy>.Instance);
        Assert.Equal(2, actual.Count);
        Assert.Equal(2, actual[1]);
        Assert.Equal(4, actual[3]);

        var serialized = CborSerializer.Serialize(
            new Dictionary<int, int> { [1] = 2, [3] = 4 },
            DictProxy.Ser<int, int, I32Proxy, I32Proxy>.Instance);
        Assert.Equal(hex, Convert.ToHexString(serialized), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_MixedValueTypes()
    {
        // Vector 68: {"a": 1, "b": [2, 3]}
        var hex = "a26161016162820203";
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<MixedMap>(bytes);
        Assert.Equal(1, actual.A);
        Assert.Equal(new[] { 2, 3 }, actual.B);

        var value = new MixedMap { A = 1, B = new[] { 2, 3 } };
        var serialized = CborSerializer.Serialize(value);
        Assert.Equal(hex, Convert.ToHexString(serialized), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Map_StringToString()
    {
        // Vector 70: {"a": "A", "b": "B", "c": "C", "d": "D", "e": "E"}
        var hex = "a56161614161626142616361436164614461656145";
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<FiveStringMap>(bytes);
        Assert.Equal("A", actual.A);
        Assert.Equal("B", actual.B);
        Assert.Equal("C", actual.C);
        Assert.Equal("D", actual.D);
        Assert.Equal("E", actual.E);

        var value = new FiveStringMap { A = "A", B = "B", C = "C", D = "D", E = "E" };
        var serialized = CborSerializer.Serialize(value);
        Assert.Equal(hex, Convert.ToHexString(serialized), StringComparer.OrdinalIgnoreCase);
    }

    // ── Tagged Date/Time (Major Type 6, Tags 0 and 1) ──────────
    // Vectors 47–49
    // Vectors 50–52 (tags 23, 24, 32) not supported — skipped

    [Fact]
    public void Tag0_DateTimeString()
    {
        // Vector 47: 0("2013-03-21T20:04:00Z")
        var hex = "c074323031332d30332d32315432303a30343a30305a";
        var expected = new DateTimeOffset(2013, 3, 21, 20, 4, 0, TimeSpan.Zero);

        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<DateTimeOffset, IDeserialize<DateTimeOffset>>(
            bytes, DateTimeOffsetProxy.Instance);
        Assert.Equal(expected, actual);

        var serialized = CborSerializer.Serialize(expected, DateTimeOffsetProxy.Instance);
        Assert.Equal(hex, Convert.ToHexString(serialized), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tag1_EpochInteger()
    {
        // Vector 48: 1(1363896240)
        var hex = "c11a514b67b0";
        var expected = DateTimeOffset.FromUnixTimeSeconds(1363896240);

        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<DateTimeOffset, IDeserialize<DateTimeOffset>>(
            bytes, DateTimeOffsetProxy.Instance);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tag1_EpochFloat()
    {
        // Vector 49: 1(1363896240.5)
        var hex = "c1fb41d452d9ec200000";
        var expected = new DateTimeOffset(2013, 3, 21, 20, 4, 0, 500, TimeSpan.Zero);

        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<DateTimeOffset, IDeserialize<DateTimeOffset>>(
            bytes, DateTimeOffsetProxy.Instance);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Tag0_DateTimeOffset_PreservesOffset()
    {
        var dt = new DateTimeOffset(2013, 3, 21, 22, 4, 0, TimeSpan.FromHours(2));
        var serialized = CborSerializer.Serialize(dt, DateTimeOffsetProxy.Instance);
        var deserialized = CborSerializer.Deserialize<DateTimeOffset, IDeserialize<DateTimeOffset>>(
            serialized, DateTimeOffsetProxy.Instance);
        Assert.Equal(dt, deserialized);
        Assert.Equal(TimeSpan.FromHours(2), deserialized.Offset);
    }

    [Fact]
    public void Tag0_DateTime_UtcRoundTrip()
    {
        var dt = new DateTime(2013, 3, 21, 20, 4, 0, DateTimeKind.Utc);
        var serialized = CborSerializer.Serialize(dt, DateTimeProxy.Instance);
        var deserialized = CborSerializer.Deserialize<DateTime, IDeserialize<DateTime>>(
            serialized, DateTimeProxy.Instance);
        Assert.Equal(dt, deserialized);
        Assert.Equal(DateTimeKind.Utc, deserialized.Kind);
    }

    [Fact]
    public void DateTime_NonUtc_Throws()
    {
        var local = new DateTime(2013, 3, 21, 20, 4, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() =>
            CborSerializer.Serialize(local, DateTimeProxy.Instance));

        var unspecified = new DateTime(2013, 3, 21, 20, 4, 0, DateTimeKind.Unspecified);
        Assert.Throws<ArgumentException>(() =>
            CborSerializer.Serialize(unspecified, DateTimeProxy.Instance));
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static void AssertRoundTrip<T, TProxy>(string hex, T expected, TProxy proxy)
        where TProxy : ISerialize<T>, IDeserializeProvider<T>
    {
        AssertDeserializeViaProvider<T, TProxy>(hex, expected);
        AssertSerialize(expected, hex, proxy);
    }

    private static void AssertDeserializeViaProvider<T, TProxy>(string hex, T expected)
        where TProxy : IDeserializeProvider<T>
    {
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<T, IDeserialize<T>>(bytes, TProxy.Instance);
        Assert.Equal(expected, actual);
    }

    private static void AssertDeserialize<T, TProxy>(string hex, T expected, TProxy proxy)
        where TProxy : IDeserialize<T>
    {
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<T, TProxy>(bytes, proxy);
        Assert.Equal(expected, actual);
    }

    private static void AssertSerialize<T, TProxy>(T value, string expectedHex, TProxy proxy)
        where TProxy : ISerialize<T>
    {
        var actual = CborSerializer.Serialize(value, proxy);
        Assert.Equal(expectedHex, Convert.ToHexString(actual), StringComparer.OrdinalIgnoreCase);
    }

    private static void AssertCollectionRoundTrip<T, TSer, TDe>(string hex, T expected)
        where TSer : ISerializeProvider<T>
        where TDe : IDeserializeProvider<T>
    {
        var bytes = Convert.FromHexString(hex);
        var actual = CborSerializer.Deserialize<T, IDeserialize<T>>(bytes, TDe.Instance);
        Assert.Equal(expected, actual);

        var serialized = CborSerializer.Serialize(expected, TSer.Instance);
        Assert.Equal(hex, Convert.ToHexString(serialized), StringComparer.OrdinalIgnoreCase);
    }
}
