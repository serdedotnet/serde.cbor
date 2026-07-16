using System.Buffers;

namespace Serde.Cbor;

partial class CborReader<TReader>
{
    private struct DeserializeType(CborReader<TReader> deserializer, int? mapLength)
        : ITypeDeserializer
    {
        int? ITypeDeserializer.SizeOpt => mapLength;

        private int _count;

        bool ITypeDeserializer.ReadBool(ISerdeInfo info, int index) => deserializer.ReadBool();

        byte ITypeDeserializer.ReadU8(ISerdeInfo info, int index) => deserializer.ReadU8();

        char ITypeDeserializer.ReadChar(ISerdeInfo info, int index) => (char)deserializer.ReadU16();

        decimal ITypeDeserializer.ReadDecimal(ISerdeInfo info, int index) =>
            deserializer.ReadDecimal();

        double ITypeDeserializer.ReadF64(ISerdeInfo info, int index) => deserializer.ReadF64();

        float ITypeDeserializer.ReadF32(ISerdeInfo info, int index) => deserializer.ReadF32();

        short ITypeDeserializer.ReadI16(ISerdeInfo info, int index) => deserializer.ReadI16();

        int ITypeDeserializer.ReadI32(ISerdeInfo info, int index) => deserializer.ReadI32();

        long ITypeDeserializer.ReadI64(ISerdeInfo info, int index) => deserializer.ReadI64();

        sbyte ITypeDeserializer.ReadI8(ISerdeInfo info, int index) => deserializer.ReadI8();

        string ITypeDeserializer.ReadString(ISerdeInfo info, int index) =>
            deserializer.ReadString();

        ushort ITypeDeserializer.ReadU16(ISerdeInfo info, int index) => deserializer.ReadU16();

        uint ITypeDeserializer.ReadU32(ISerdeInfo info, int index) => deserializer.ReadU32();

        ulong ITypeDeserializer.ReadU64(ISerdeInfo info, int index) => deserializer.ReadU64();

        T ITypeDeserializer.ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) =>
            deserialize.Deserialize(deserializer);

        int ITypeDeserializer.ReadEnum(ISerdeInfo typeInfo, int index, ISerdeInfo fieldInfo) =>
            ((IDeserializer)deserializer).ReadEnum(fieldInfo);

        IDeserializer ITypeDeserializer.ReadFieldStart(ISerdeInfo info, int index) => deserializer;

        void ITypeDeserializer.ReadFieldEnd(
            ISerdeInfo info,
            int index,
            IDeserializer deserializer
        ) { }

        void ITypeDeserializer.SkipValue(ISerdeInfo info, int index) =>
            throw new NotImplementedException();

        int ITypeDeserializer.TryReadIndex(ISerdeInfo map)
        {
            if (map.Kind is InfoKind.CustomType or InfoKind.Union)
            {
                // Honor the actual number of pairs on the wire, not the type's field
                // count: the serializer may omit fields (e.g. null members), so the wire
                // map can be shorter than FieldCount. A union is a single-pair map keyed
                // by the case name.
                if (_count >= (mapLength ?? map.FieldCount))
                {
                    return ITypeDeserializer.EndOfType;
                }
                var span = deserializer.ReadUtf8Span();
                int index = map.TryGetIndex(span);
                _count++;
                return index;
            }
            else
            {
                return ITypeDeserializer.IndexNotFound;
            }
        }

        (int, string?) ITypeDeserializer.TryReadIndexWithName(ISerdeInfo map)
        {
            if (map.Kind is InfoKind.CustomType or InfoKind.Union)
            {
                if (_count >= (mapLength ?? map.FieldCount))
                {
                    return (ITypeDeserializer.EndOfType, null);
                }
                var span = deserializer.ReadUtf8Span();
                int index = map.TryGetIndex(span);
                string? errorName =
                    index == ITypeDeserializer.IndexNotFound ? span.ToString() : null;
                _count++;
                return (index, errorName);
            }
            else
            {
                return (
                    ITypeDeserializer.IndexNotFound,
                    "Expected a custom type, found: " + map.Kind
                );
            }
        }

        UInt128 ITypeDeserializer.ReadU128(ISerdeInfo info, int index) => deserializer.ReadU128();

        Int128 ITypeDeserializer.ReadI128(ISerdeInfo info, int index) => deserializer.ReadI128();

        DateTimeOffset ITypeDeserializer.ReadDateTimeOffset(ISerdeInfo info, int index) =>
            deserializer.ReadDateTimeOffset();

        DateTime ITypeDeserializer.ReadDateTime(ISerdeInfo info, int index) =>
            deserializer.ReadDateTime();

        void ITypeDeserializer.ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer)
        {
            deserializer.ReadBytes(writer);
        }
    }
}
