using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NativeAot;
using Benchmarks;
using Serde.Cbor;

// Correctness check: a value should survive a CBOR round trip unchanged.
var bytes = CborSerializer.Serialize(Location.Sample);
var roundTripped = CborSerializer.Deserialize<Location>(bytes);

Console.WriteLine("Checking correctness of serialization: " + (Location.Sample == roundTripped));
if (Location.Sample != roundTripped)
{
    throw new InvalidOperationException(
        $"""
Round trip is not correct
Original:
{Location.Sample}

Deserialized:
{roundTripped}
"""
    );
}

var nativeAotToolchain = NativeAotToolchain
    .CreateBuilder()
    .UseNuGet("10.0.8")
    .TargetFrameworkMoniker("net10.0")
    .ToToolchain();

var config = DefaultConfig
    .Instance.AddJob(Job.Default.WithId("NativeAOT").WithToolchain(nativeAotToolchain))
    .AddDiagnoser(MemoryDiagnoser.Default);
var summary = BenchmarkSwitcher
    .FromAssembly(typeof(DeserializeFromString<>).Assembly)
    .Run(args, config);
