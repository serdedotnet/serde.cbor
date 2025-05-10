
using System.Buffers;

namespace Serde.Cbor;

partial class CborReader<TReader>
{
    private struct DeserializeCollection(CborReader<TReader> deserializer, bool isDict, int length) : ITypeDeserializer
    {
        private int _index;
        int? ITypeDeserializer.SizeOpt => isDict switch
        {
            true => length / 2,
            false => length,
        };

        bool ITypeDeserializer.ReadBool(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadBool();
            _index++;
            return v;
        }

        char ITypeDeserializer.ReadChar(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadChar();
            _index++;
            return v;
        }

        decimal ITypeDeserializer.ReadDecimal(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadDecimal();
            _index++;
            return v;
        }

        float ITypeDeserializer.ReadF32(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadF32();
            _index++;
            return v;
        }

        double ITypeDeserializer.ReadF64(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadF64();
            _index++;
            return v;
        }

        short ITypeDeserializer.ReadI16(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadI16();
            _index++;
            return v;
        }

        int ITypeDeserializer.ReadI32(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadI32();
            _index++;
            return v;
        }

        long ITypeDeserializer.ReadI64(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadI64();
            _index++;
            return v;
        }

        sbyte ITypeDeserializer.ReadI8(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadI8();
            _index++;
            return v;
        }

        string ITypeDeserializer.ReadString(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadString();
            _index++;
            return v;
        }

        ushort ITypeDeserializer.ReadU16(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadU16();
            _index++;
            return v;
        }

        uint ITypeDeserializer.ReadU32(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadU32();
            _index++;
            return v;
        }

        ulong ITypeDeserializer.ReadU64(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadU64();
            _index++;
            return v;
        }

        byte ITypeDeserializer.ReadU8(ISerdeInfo info, int index)
        {
            var v = deserializer.ReadU8();
            _index++;
            return v;
        }

        int ITypeDeserializer.TryReadIndex(ISerdeInfo info, out string? errorName)
        {
            errorName = null;
            if (_index >= length)
            {
                return ITypeDeserializer.EndOfType;
            }
            return _index;
        }

        T ITypeDeserializer.ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> d)
        {
            var next = d.Deserialize(deserializer);
            _index++;
            return next;
        }

        void ITypeDeserializer.SkipValue(ISerdeInfo info, int index)
        {
            throw new NotImplementedException();
        }

        DateTime ITypeDeserializer.ReadDateTime(ISerdeInfo info, int index)
        {
            var next = deserializer.ReadDateTime();
            _index++;
            return next;
        }

        void ITypeDeserializer.ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
        {
            deserializer.ReadBytes(writer);
            _index++;
        }
    }
}