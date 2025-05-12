using System.Buffers;

namespace Serde.Cbor;

partial class CborReader<TReader>
{
    private sealed class EnumDeserializer(CborReader<TReader> reader) : ITypeDeserializer
    {
        public int? SizeOpt => null;
        public bool ReadBool(ISerdeInfo info, int index) => reader.ReadBool();
        public void ReadBytes(ISerdeInfo info, int index, IBufferWriter<byte> writer) => reader.ReadBytes(writer);
        public char ReadChar(ISerdeInfo info, int index) => reader.ReadChar();
        public DateTime ReadDateTime(ISerdeInfo info, int index) => reader.ReadDateTime();
        public decimal ReadDecimal(ISerdeInfo info, int index) => reader.ReadDecimal();
        public float ReadF32(ISerdeInfo info, int index) => reader.ReadF32();
        public double ReadF64(ISerdeInfo info, int index) => reader.ReadF64();
        public short ReadI16(ISerdeInfo info, int index) => reader.ReadI16();
        public int ReadI32(ISerdeInfo info, int index) => reader.ReadI32();
        public long ReadI64(ISerdeInfo info, int index) => reader.ReadI64();
        public sbyte ReadI8(ISerdeInfo info, int index) => reader.ReadI8();
        public string ReadString(ISerdeInfo info, int index) => reader.ReadString();
        public ushort ReadU16(ISerdeInfo info, int index) => reader.ReadU16();
        public uint ReadU32(ISerdeInfo info, int index) => reader.ReadU32();
        public ulong ReadU64(ISerdeInfo info, int index) => reader.ReadU64();

        public byte ReadU8(ISerdeInfo info, int index) => reader.ReadU8();

        public T ReadValue<T>(ISerdeInfo info, int index, IDeserialize<T> deserialize) where T : class?
            => deserialize.Deserialize(reader);

        public void SkipValue(ISerdeInfo info, int index)
        {
            throw new NotImplementedException();
        }

        public int TryReadIndex(ISerdeInfo info, out string? errorName)
        {
            // Enums are serialized as the index of the enum member
            errorName = null;
            return reader.ReadI32();
        }
    }
}