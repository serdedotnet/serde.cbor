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
        private byte[] value = null!;

        private readonly IDeserialize<T> _proxy = T.Instance;

        [GlobalSetup]
        public void Setup()
        {
            value = DataGenerator.GenerateDeserialize<T>();
        }

        [Benchmark]
        public T? MessagePack()
        {
            return MessagePackSerializer.Deserialize<T>(value);
        }

        [Benchmark]
        public T SerdeMsgPack() => Serde.Cbor.MsgPackSerializer.Deserialize<T, IDeserialize<T>>(value, _proxy);
    }
}