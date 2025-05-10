using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using Benchmarks;
using MessagePack;

var msg1 = MessagePackSerializer.Serialize(Location.Sample);
var msg2 = Serde.Cbor.MsgPackSerializer.Serialize(Location.Sample);

if (!msg1.SequenceEqual(msg2))
{
    Console.WriteLine(string.Join(", ", msg1));
    Console.WriteLine(string.Join(", ", msg2));
    throw new InvalidOperationException("bytes do not match");
}

var loc1 = MessagePackSerializer.Deserialize<Location>(msg1);
var loc2 = Serde.Cbor.MsgPackSerializer.Deserialize<Location>(msg1);

Console.WriteLine("Checking correctness of serialization: " + (loc1 == loc2));
if (loc1 != loc2)
{
    throw new InvalidOperationException($"""
Serialization is not correct
STJ:
{loc1}

Serde:
{loc2}
""");
}

var config = DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default);
var summary = BenchmarkSwitcher.FromAssembly(typeof(DeserializeFromString<>).Assembly)
    .Run(args, config);