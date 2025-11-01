# NextId
Strongly-typed, K-Sortable globally unique identifier with checksum for .NET 8+.

---

## Example

Example value: 
```
user-222v7NyttKhf2dvcpStpdNKD9TW
```

- Left from `-` is type (max 11 characters)
- Right from `-` is id value  

id is componsed of:
  - First 12 chars, Unix timestamp in microseconds
  - Next 12 chars, random value
  - Final 3 chars, checksum value

Checksum can be used to validate id before going to database to detect malicious or 
unexpected activities by clients.

---

`NumberValue` can be used to avoid displaying Id as random string which can unintentionally contain words. 
It also has obfuscated time component by xoring with hash of Salt value and reversing resulting string.
Obfuscation is done to prevent easily guessing time component of the id.

Example number value:
```
user-9646185430515823890035197343360748348694018675
```


---

## How to use it

Install [nuget package](https://www.nuget.org/packages/NextId)

```
dotnet add package NextId
```

Optionally add reference to `NextId.Gen` to use source generator.

### Using NextId with Source Generator

Add reference to `NextId.Gen` to use source generator.
Source generator is available since version 2.

```xml
<PackageReference Include="NextId.Gen" 
                  Version="[2.*, 3)"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  />
```

Optionally: set `Version` to be fixed, e.g. `"2.0.0"`

Create partial class for id:

```csharp
[Identifier(Prefix = "user", Salt = "99AAB45utg")]
public partial class UserId;
```

Source generator will create boilerplate code as in example in next section.

### Using NextId without Source Generator

Inherit `NextId.Identifier` class:

```csharp
public class UserId : Identifier<UserId>, IParsable<UserId>
{
    private const string PrefixConst = "user";
    private const string SaltConst = "99AAB45utg";
    protected override string Prefix => PrefixConst;
    protected override string Salt => SaltConst;

    private UserId() { }
    
    // constructor with time component
    public UserId(DateTimeOffset dt) : base(dt) { }

    // constructor for parsing
    private UserId(string value) : base(value) { }

    public static UserId NewId() => new();

    public static UserId Parse(string s) => Parse(s, null);

    public static UserId Parse(string s, IFormatProvider? provider) => new(s);

    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)]out UserId? result)
    {
        try
        {
            result = Parse(s!, provider);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
}
```

```csharp
UserId id1 = UserId.NewId();
UserId id2 = UserId.Parse(id1.Value);
UserId id3 = UserId.Parse(id1.NumberValue);

(id1 == id2).Should().BeTrue();
(id1 == id3).Should().BeTrue();
```

You can get string value of identifier by calling `Value` property or `ToString()` method.

Prefix will be set as identifier prefix (`user` in this case). 
Value must have less than 12 characters and can contain only ASCII letters and digits.

Salt must be less than 33 characters and should be set to random value for each type.
Once set, it must not be changed, otherwise parsing of existing values will fail.

## Serialization

`System.Text.Json` serializer options are available.

First add package reference:

```
dotnet add package NextId.Serialization.Json
```

```csharp
var options = new JsonSerializerOptions();
// set serializeIdsAsNumberValues to true to serialize ids as `NumberValue`
options.AddIdentifierConverters(serializeIdsAsNumberValues: false);

User user1 = User.NewRandomUser();
string json = JsonSerializer.Serialize(user1, options);
User user2 = JsonSerializer.Deserialize<User>(json, options)!;
```

## Performance

**v2 performance:**

```
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=9.0.306
  [Host]     : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
```

|                 Method |     Mean |   Error |  StdDev |    Gen0 | Allocated |
|----------------------- |---------:|--------:|--------:|--------:|----------:|
|             NewId_1000 | 171.7 us | 0.85 us | 0.80 us | 45.8984 |  187.5 KB |
| NewId_1000_NumberValue | 266.4 us | 1.62 us | 1.52 us | 62.9883 | 257.81 KB |
|             Parse_1000 | 214.5 us | 0.58 us | 0.48 us | 40.0391 | 164.06 KB |

`NewId_1000` is method generating 1000 ids and getting `Value`.
`NewId_1000_NumberValue` is method generating 1000 ids and getting `NumberValue`.
`Parse_1000` is method parsing 1000 ids.

**v1 Performance:**

|                 Method |       Mean |    Error |   StdDev |
|----------------------- |-----------:|---------:|---------:|
|             NewId_1000 |   849.6 us |  4.00 us |  3.74 us |
| NewId_1000_NumberValue | 2,840.0 us | 22.17 us | 20.73 us |
|             Parse_1000 | 1,972.3 us | 18.70 us | 17.49 us |

## Changes

- 2.0.0
    -  Full rewrite, not backward compatible
    -  Replaced SHA256 with xxHash128
    -  New code allocate less memory
    -  Added source generator for boilerplate code
- 1.1.1
    - Fix bug where `IsValid` method returns `false` for `NumberValue`

- 1.1.0
    - Fixed bug when verifying validity of value
    - Added support for `NumberValue`

- 1.0.0 
    - Initial version
