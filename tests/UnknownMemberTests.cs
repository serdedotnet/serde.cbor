namespace Serde.Cbor.Tests;

public partial class UnknownMemberTests
{
    [GenerateSerde]
    public partial record WithNull
    {
        public string? Name { get; init; }
        public int Value { get; init; }
    }

    // A member of every shape the writer produces, so that skipping them as unknown members covers
    // each
    [GenerateSerde]
    public partial record Wide
    {
        [SerdeMemberOptions(SerializeNull = true)]
        public int? Null { get; init; }
        public bool Flag { get; init; }
        public long Big { get; init; }
        public int Negative { get; init; }
        public double Number { get; init; }
        public string Text { get; init; } = "";
        public byte[] Bytes { get; init; } = [];
        public List<int> List { get; init; } = [];
        public Dictionary<string, int> Map { get; init; } = [];
        public WithNull Nested { get; init; } = new();
        public int A { get; init; }
        public string Tail { get; init; } = "";
    }

    [GenerateSerde]
    public partial record Narrow
    {
        public int A { get; init; }
    }

    [GenerateSerde]
    [SerdeTypeOptions(DenyUnknownMembers = true)]
    public partial record DenyNarrow
    {
        public int A { get; init; }
    }

    [GenerateSerde]
    public partial record WithOptional
    {
        public int A { get; init; }
        public string? Name { get; init; }
    }

    [GenerateSerde]
    public partial record OuterWide
    {
        public Wide Inner { get; init; } = new();
        public int After { get; init; }
    }

    [GenerateSerde]
    public partial record OuterNarrow
    {
        public Narrow Inner { get; init; } = new();
        public int After { get; init; }
    }

    private static readonly Wide s_wide = new()
    {
        Flag = true,
        Big = long.MaxValue,
        Negative = -1000,
        Number = 1.5,
        Text = new string('x', 40),
        Bytes = [1, 2, 3],
        List = [1, 2],
        Map = new() { ["k"] = 1 },
        Nested = new WithNull { Name = "n", Value = 1 },
        A = 42,
        Tail = "t",
    };

    [Fact]
    public void NullMemberIsOmitted()
    {
        var value = new WithNull { Name = null, Value = 7 };
        var bytes = CborSerializer.Serialize(value);
        Assert.Equal(0xa1, bytes[0]); // a map with one entry
        Assert.Equal(value, CborSerializer.Deserialize<WithNull>(bytes));
    }

    [Fact]
    public void UnknownMembersAreSkipped()
    {
        var narrow = CborSerializer.Deserialize<Narrow>(CborSerializer.Serialize(s_wide));
        Assert.Equal(42, narrow.A);
    }

    // Items the writer never produces, but other CBOR encoders can
    [Fact]
    public void UnknownIndefiniteLengthAndTaggedItemsAreSkipped()
    {
        byte[] bytes =
        [
            0xa9, // a map with 9 entries
            0x61, (byte)'x', 0x9f, 0x01, 0x02, 0xff, // indefinite-length array [_ 1, 2]
            0x61, (byte)'y', 0x7f, 0x62, (byte)'a', (byte)'b', 0x61, (byte)'c', 0xff, // (_ "ab", "c")
            0x61, (byte)'z', 0xbf, 0x61, (byte)'k', 0x01, 0xff, // indefinite-length map {_ "k": 1}
            0x61, (byte)'b', 0x5f, 0x41, 0x01, 0x42, 0x02, 0x03, 0xff, // (_ h'01', h'0203')
            0x61, (byte)'t', 0xc1, 0x19, 0x03, 0xe8, // tag 1 (epoch time) 1000
            0x61, (byte)'h', 0xf9, 0x3c, 0x00, // half-precision float 1.0
            0x61, (byte)'u', 0xf7, // undefined
            0x61, (byte)'s', 0xf8, 0x20, // simple value 32
            0x61, (byte)'a', 0x05, // the known member
        ];
        Assert.Equal(5, CborSerializer.Deserialize<Narrow>(bytes).A);
    }

    [Fact]
    public void SkippingConsumesExactlyTheUnknownMembers()
    {
        var bytes = CborSerializer.Serialize(new OuterWide { Inner = s_wide, After = 9 });
        var outer = CborSerializer.Deserialize<OuterNarrow>(bytes);
        Assert.Equal(42, outer.Inner.A);
        Assert.Equal(9, outer.After);
    }

    [Fact]
    public void OmittedMembersAreAllowed()
    {
        var bytes = CborSerializer.Serialize(new Narrow { A = 1 });
        Assert.Equal(
            new WithOptional { A = 1, Name = null },
            CborSerializer.Deserialize<WithOptional>(bytes)
        );
    }

    [Fact]
    public void DeniedUnknownMemberThrows()
    {
        var bytes = CborSerializer.Serialize(s_wide);
        Assert.Throws<DeserializeException>(() => CborSerializer.Deserialize<DenyNarrow>(bytes));
    }
}
