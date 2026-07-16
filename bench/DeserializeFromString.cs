// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MessagePack;
using Serde;

namespace Benchmarks
{
    [GenericTypeArguments(typeof(Location))]
    public class DeserializeFromString<T>
        where T : Serde.IDeserializeProvider<T>
    {
        private byte[] _msgPackBytes = null!;
        private byte[] _cborBytes = null!;

        private readonly IDeserialize<T> _proxy = T.Instance;

        [GlobalSetup]
        public void Setup()
        {
            _msgPackBytes = DataGenerator.GenerateMessagePackBytes<T>();
            _cborBytes = DataGenerator.GenerateCborBytes<T>();
        }

        [Benchmark]
        public T? MessagePack()
        {
            return MessagePackSerializer.Deserialize<T>(_msgPackBytes);
        }

        [Benchmark]
        public T SerdeCbor() =>
            Serde.Cbor.CborSerializer.Deserialize<T, IDeserialize<T>>(_cborBytes, _proxy);
    }
}
