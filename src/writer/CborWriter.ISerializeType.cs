
using Serde;

namespace Serde.Cbor;

partial class CborWriter : ITypeSerializer
{
    private void WritePropertyName(ISerdeInfo typeInfo, int fieldIndex)
    {
        var fieldName = typeInfo.GetFieldName(fieldIndex);
        WriteUtf8(fieldName);
    }

    void ITypeSerializer.End(ISerdeInfo typeInfo)
    {
        // No end, all types are length-prefixed
    }

    void ITypeSerializer.WriteValue<T>(ISerdeInfo typeInfo, int fieldIndex, T value, ISerialize<T> serialize)
    {
        WritePropertyName(typeInfo, fieldIndex);
        serialize.Serialize(value, this);
    }

    void ITypeSerializer.WriteBool(ISerdeInfo typeInfo, int index, bool b)
    {
        WritePropertyName(typeInfo, index);
        WriteBool(b);
    }

    void ITypeSerializer.WriteChar(ISerdeInfo typeInfo, int index, char c)
    {
        WritePropertyName(typeInfo, index);
        WriteChar(c);
    }

    void ITypeSerializer.WriteU8(ISerdeInfo typeInfo, int index, byte b)
    {
        WritePropertyName(typeInfo, index);
        WriteU8(b);
    }

    void ITypeSerializer.WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
    {
        WritePropertyName(typeInfo, index);
        WriteU16(u16);
    }

    void ITypeSerializer.WriteU32(ISerdeInfo typeInfo, int index, uint u32)
    {
        WritePropertyName(typeInfo, index);
        WriteU32(u32);
    }

    void ITypeSerializer.WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
    {
        WritePropertyName(typeInfo, index);
        WriteU64(u64);
    }

    void ITypeSerializer.WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
    {
        WritePropertyName(typeInfo, index);
        WriteI8(b);
    }

    void ITypeSerializer.WriteI16(ISerdeInfo typeInfo, int index, short i16)
    {
        WritePropertyName(typeInfo, index);
        WriteI16(i16);
    }

    void ITypeSerializer.WriteI32(ISerdeInfo typeInfo, int index, int i32)
    {
        WritePropertyName(typeInfo, index);
        WriteI32(i32);
    }

    void ITypeSerializer.WriteI64(ISerdeInfo typeInfo, int index, long i64)
    {
        WritePropertyName(typeInfo, index);
        WriteI64(i64);
    }

    void ITypeSerializer.WriteF32(ISerdeInfo typeInfo, int index, float f)
    {
        WritePropertyName(typeInfo, index);
        WriteF32(f);
    }

    void ITypeSerializer.WriteF64(ISerdeInfo typeInfo, int index, double d)
    {
        WritePropertyName(typeInfo, index);
        WriteF64(d);
    }

    void ITypeSerializer.WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
    {
        WritePropertyName(typeInfo, index);
        WriteDecimal(d);
    }

    void ITypeSerializer.WriteString(ISerdeInfo typeInfo, int index, string s)
    {
        WritePropertyName(typeInfo, index);
        WriteString(s);
    }

    void ITypeSerializer.WriteNull(ISerdeInfo typeInfo, int index)
    {
        WritePropertyName(typeInfo, index);
        WriteNull();
    }

    void ITypeSerializer.WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dt)
    {
        WritePropertyName(typeInfo, index);
        WriteDateTimeOffset(dt);
    }

    void ITypeSerializer.WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dt)
    {
        WritePropertyName(typeInfo, index);
        WriteDateTime(dt);
    }

    void ITypeSerializer.WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
    {
        WritePropertyName(typeInfo, index);
        WriteBytes(bytes);
    }
}