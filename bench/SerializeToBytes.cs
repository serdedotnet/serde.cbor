// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MessagePack;
using Serde;

namespace Benchmarks
{
    [GenericTypeArguments(typeof(Location))]
    public class SerializeToBytes<T>
        where T : Serde.ISerializeProvider<T>
    {
        private T value = default!;

        private readonly ISerialize<T> _proxy = T.Instance;

        [GlobalSetup]
        public void Setup()
        {
            value = DataGenerator.GenerateSerialize<T>();
        }

        [Benchmark]
        public byte[] MessagePack()
        {
            return MessagePackSerializer.Serialize(value);
        }

        [Benchmark]
        public byte[] SerdeMsgPack() => Serde.Cbor.MsgPackSerializer.Serialize(value, _proxy);
    }
}