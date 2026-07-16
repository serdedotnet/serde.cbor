using System;

namespace Benchmarks
{
    internal static class DataGenerator
    {
        public static T GenerateSerialize<T>()
            where T : Serde.ISerializeProvider<T>
        {
            if (typeof(T) == typeof(LoginViewModel))
                return (T)(object)CreateLoginViewModel();
            if (typeof(T) == typeof(Location))
                return (T)(object)Location.Sample;

            throw new InvalidOperationException();
        }

        public static byte[] GenerateMessagePackBytes<T>()
        {
            if (typeof(T) == typeof(LoginViewModel))
                return MessagePack.MessagePackSerializer.Serialize(CreateLoginViewModel());
            if (typeof(T) == typeof(Location))
                return MessagePack.MessagePackSerializer.Serialize(Location.Sample);

            throw new InvalidOperationException("Unexpected type");
        }

        public static byte[] GenerateCborBytes<T>()
        {
            if (typeof(T) == typeof(LoginViewModel))
                return Serde.Cbor.CborSerializer.Serialize(CreateLoginViewModel());
            if (typeof(T) == typeof(Location))
                return Serde.Cbor.CborSerializer.Serialize(Location.Sample);

            throw new InvalidOperationException("Unexpected type");
        }

        private static LoginViewModel CreateLoginViewModel() =>
            new LoginViewModel
            {
                Email = "name.familyname@not.com",
                // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy credentials for perf testing.")]
                Password = "abcdefgh123456!@",
                RememberMe = true,
            };
    }
}
