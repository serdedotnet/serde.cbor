# Serde.Cbor

A [CBOR](https://cbor.io/) (Concise Binary Object Representation, [RFC 8949](https://www.rfc-editor.org/rfc/rfc8949.html)) serializer and deserializer for .NET, built on the [Serde](https://github.com/serdedotnet/serde) serialization framework.

Serde.Cbor is an extremely fast, AOT-compatible serializer/deserializer library for .NET 10+ with a simple declaritive attribute-based model.

## Installation

```sh
dotnet add package Serde.Cbor
```

## Usage

Use the `[GenerateSerde]` attribute from the Serde framework to generate serialization code for your types:

```csharp
using Serde;
using Serde.Cbor;

[GenerateSerde]
public partial record Person
{
    public required string Name { get; init; }
    public int Age { get; init; }
}

var person = new Person { Name = "Alice", Age = 30 };
byte[] bytes = CborSerializer.Serialize(person);
Person deserialized = CborSerializer.Deserialize<Person>(bytes);
```

## Related projects

- [Serde](https://github.com/serdedotnet/serde) — the underlying serialization framework

## License

BSD-3-Clause. See [LICENSE](LICENSE) for details.
