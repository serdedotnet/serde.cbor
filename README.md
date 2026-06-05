# Serde.Cbor

A [CBOR](https://cbor.io/) (Concise Binary Object Representation, [RFC 8949](https://www.rfc-editor.org/rfc/rfc8949.html)) serializer and deserializer for .NET, built on the [Serde](https://github.com/serdedotnet/serde) serialization framework.

CBOR is a binary data format designed for small message size, making it ideal for network protocols, embedded systems, and anywhere compact binary encoding is needed.

## Installation

```sh
dotnet add package Serde.Cbor
```

## Usage

### Serialize and deserialize simple types

```csharp
using Serde.Cbor;

byte[] bytes = CborSerializer.Serialize(42);
int value = CborSerializer.Deserialize<int>(bytes);
```

### Serialize and deserialize custom types

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

### Collections

Arrays, lists, and dictionaries are supported:

```csharp
byte[] bytes = CborSerializer.Serialize(new[] { 1, 2, 3 });
int[] values = CborSerializer.Deserialize<int[]>(bytes);
```

## Supported types

- Integers: `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`
- Floating point: `float`, `double`
- `bool`, `char`, `string`
- `DateTime`, `DateTimeOffset`
- Byte arrays
- Nullable reference types
- Arrays and dictionaries
- Custom types via `[GenerateSerde]`
- Enums

## Related projects

- [Serde](https://github.com/serdedotnet/serde) — the underlying serialization framework

## License

BSD-3-Clause. See [LICENSE](LICENSE) for details.
