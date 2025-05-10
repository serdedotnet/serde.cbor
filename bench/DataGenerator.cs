
using System;

namespace Benchmarks
{
    internal static class DataGenerator
    {
        public static T GenerateSerialize<T>() where T : Serde.ISerializeProvider<T>
        {
            if (typeof(T) == typeof(LoginViewModel))
                return (T)(object)CreateLoginViewModel();
            if (typeof(T) == typeof(Location))
                return (T)(object)Location.Sample;

            throw new InvalidOperationException();

            static LoginViewModel CreateLoginViewModel() => new LoginViewModel
            {
                Email = "name.familyname@not.com",
                // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Dummy credentials for perf testing.")]
                Password = "abcdefgh123456!@",
                RememberMe = true
            };

        }

        public static byte[] GenerateDeserialize<T>()
        {
            if (typeof(T) == typeof(LoginViewModel))
                return MessagePack.MessagePackSerializer.Serialize(LoginViewSample);
            if (typeof(T) == typeof(Location))
                return MessagePack.MessagePackSerializer.Serialize(Location.Sample);

            throw new InvalidOperationException("Unexpected type");
        }

        public const string LoginViewSample = """
{
    "email": "name.familyname@not.com",
    "password": "abcdefgh123456!@",
    "rememberMe": true
}
""";

    }
}