# NextId
Strongly-typed, K-Sortable globally unique identifier with checksum for .NET 8+.

---

## Example

Example value: 
```
user-222v7HzzwZTa3p6STTBZHqdqaYj
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
user-4749359544740341583108563723412048386425073886
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

|     Method |     Mean |   Error |  StdDev |
|----------- |---------:|--------:|--------:|
| NewId_1000 | 585.1 us | 2.71 us | 2.54 us |
| Parse_1000 | 660.3 us | 4.19 us | 3.92 us |

`NewId_1000` is method generating 1000 ids. `Parse_1000` is method parsing 1000 ids.

**v1 Performance:**

|     Method |       Mean |   Error |  StdDev |
|----------- |-----------:|--------:|--------:|
| NewId_1000 |   850.3 us | 3.59 us | 3.18 us |
| Parse_1000 | 1,995.5 us | 8.10 us | 7.18 us |

## Changes

- 2.0.0
    -  Rewrite using fixed lenght ids and source generator
- 1.1.1
    - Fix bug where `IsValid` method returns `false` for `NumberValue`

- 1.1.0
    - Fixed bug when verifying validity of value
    - Added support for `NumberValue`

- 1.0.0 
    - Initial version
