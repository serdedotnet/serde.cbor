
namespace Serde.Cbor;

partial class CborWriter
{
    private sealed class SerCollection(CborWriter writer) : ITypeSerializer
    {
        public void End(ISerdeInfo typeInfo)
        {
            // No action needed, all collections are length-prefixed
        }

        public void WriteBool(ISerdeInfo typeInfo, int index, bool b)
            => writer.WriteBool(b);

        public void WriteChar(ISerdeInfo typeInfo, int index, char c)
            => writer.WriteChar(c);

        public void WriteDecimal(ISerdeInfo typeInfo, int index, decimal d)
            => writer.WriteDecimal(d);

        public void WriteF32(ISerdeInfo typeInfo, int index, float f)
            => writer.WriteF32(f);

        public void WriteF64(ISerdeInfo typeInfo, int index, double d)
            => writer.WriteF64(d);

        public void WriteI16(ISerdeInfo typeInfo, int index, short i16)
            => writer.WriteI16(i16);

        public void WriteI32(ISerdeInfo typeInfo, int index, int i32)
            => writer.WriteI32(i32);

        public void WriteI64(ISerdeInfo typeInfo, int index, long i64)
            => writer.WriteI64(i64);

        public void WriteI8(ISerdeInfo typeInfo, int index, sbyte b)
            => writer.WriteI8(b);

        public void WriteNull(ISerdeInfo typeInfo, int index)
            => writer.WriteNull();

        public void WriteString(ISerdeInfo typeInfo, int index, string s)
            => writer.WriteString(s);

        public void WriteU16(ISerdeInfo typeInfo, int index, ushort u16)
            => writer.WriteU16(u16);

        public void WriteU32(ISerdeInfo typeInfo, int index, uint u32)
            => writer.WriteU32(u32);

        public void WriteU64(ISerdeInfo typeInfo, int index, ulong u64)
            => writer.WriteU64(u64);

        public void WriteU8(ISerdeInfo typeInfo, int index, byte b)
            => writer.WriteU8(b);

        public void WriteValue<T>(ISerdeInfo typeInfo, int index, T value, ISerialize<T> serialize)
            where T : class?
            => serialize.Serialize(value, writer);

        public void WriteDateTimeOffset(ISerdeInfo typeInfo, int index, DateTimeOffset dateTimeOffset)
            => writer.WriteDateTimeOffset(dateTimeOffset);

        public void WriteDateTime(ISerdeInfo typeInfo, int index, DateTime dateTime)
            => writer.WriteDateTime(dateTime);

        public void WriteBytes(ISerdeInfo typeInfo, int index, ReadOnlyMemory<byte> bytes)
            => writer.WriteBytes(bytes);
    }
}