
using System.Buffers;

namespace Serde.Cbor;

partial class CborReader<TReader>
{
    private struct DeserializeType(CborReader<TReader> deserializer) : ITypeDeserializer
    {
        int? ITypeDeserializer.SizeOpt => null;

        private int _count;

        bool ITypeDeserializer.ReadBool(ISerdeInfo info, int index) => deserializer.ReadBool();

        byte ITypeDeserializer.ReadU8(ISerdeInfo info, int index) => deserializer.ReadU8();

        char ITypeDeserializer.ReadChar(ISerdeInfo info, int index) => (char)deserializer.ReadU16();

        decimal ITypeDeserializer.ReadDecimal(ISerdeInfo info, int index) => deserializer.ReadDecimal();
        double ITypeDeserializer.ReadF64(ISerdeInfo info, int index) => deserializer.ReadF64();

        float ITypeDeserializer.ReadF32(ISerdeInfo info, int index) => deserializer.ReadF32();

        short ITypeDeserializer.ReadI16(ISerdeInfo info, int index) => deserializer.ReadI16();

        int ITypeDeserializer.ReadI32(ISerdeInfo info, int index) => deserializer.ReadI32();

        long ITypeDeserializer.ReadI64(ISerdeInfo info, int index) => deserializer.ReadI64();

        sbyte ITypeDeserializer.ReadI8(ISerdeInfo info, int index) => deserializer.ReadI8();

        string ITypeDeserializer.ReadString(ISerdeInfo info, int index) => deserializer.ReadString();

        ushort ITypeDeserializer.ReadU16(ISerdeInfo info, int index) => deserializer.ReadU16();

        uint ITypeDeserializer.ReadU32(ISerdeInfo info, int index) => deserializer.ReadU32();

        ulong ITypeDeserializer.ReadU64(ISerdeInfo info, int index) => deserializer.ReadU64();

        T ITypeDeserializer.ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize)
             => deserialize.Deserialize(deserializer);

        void ITypeDeserializer.SkipValue(ISerdeInfo info, int index)
            => throw new NotImplementedException();

        int ITypeDeserializer.TryReadIndex(ISerdeInfo map, out string? errorName)
        {
            // Two options: we have a struct/class, or an enum
            if (map.Kind == InfoKind.CustomType)
            {
                if (_count >= map.FieldCount)
                {
                    errorName = null;
                    return ITypeDeserializer.EndOfType;
                }
                // custom types are serialized like maps with field names as keys
                var span = deserializer.ReadUtf8Span();
                int index = map.TryGetIndex(span);
                errorName = index == ITypeDeserializer.IndexNotFound ? span.ToString() : null;
                _count++;
                return index;
            }
            else if (map.Kind == InfoKind.Enum)
            {
                // Enums are serialized as the index of the enum member
                errorName = null;
                return deserializer.ReadI32();
            }
            else
            {
                errorName = "Expected a custom type or enum, found: " + map.Kind;
                return ITypeDeserializer.IndexNotFound;
            }
        }

        DateTime ITypeDeserializer.ReadDateTime(ISerdeInfo info, int index)
            => deserializer.ReadDateTime();

        void ITypeDeserializer.ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
        {
            deserializer.ReadBytes(writer);
        }
    }
}