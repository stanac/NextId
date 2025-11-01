# NextId
Strongly-typed, K-Sortable globally unique identifier with checksum for .NET 7+.

---

## Example

Example value: 
```
user-4B4XH7BnCp68CCY8mzVbNT5X
```

- Left from `-` is type (max 11 characters)
- Right from `-` is id value  

id is componsed of:
  - First 8 chars, Unix timestamp in milliseconds
  - Next 2 chars, microseconds of the timestamp
  - Next 11 chars, random value
  - Final 3 chars, checksum value

Checksum can be used to validate id before going to database to detect malicious or 
unexpected activities by clients.

---

`NumberValue` can be used to avoid displaying Id as random string which can 
unintentionally contain words.

Example number value:
```
user-020802261305083909400406090927063849243018230326
```


---

## How to use it

Install [nuget package](https://www.nuget.org/packages/NextId)

```
dotnet add package NextId
```

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

Salt must be less than 33 characters and should be set to random value to each type.
Once set, it must not be changed.

## Serialization

`System.Text.Json` serializer options are available.

First add package reference:

```
dotnet add package NextId.Serialization.Json
```

```csharp
var options = new JsonSerializerOptions();
options.AddIdentifierConverters();

User user1 = User.NewRandomUser();
string json = JsonSerializer.Serialize(user1, options);
User user2 = JsonSerializer.Deserialize<User>(json, options)!;
```

## Performance

```
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=9.0.306
  [Host]     : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.21 (8.0.2125.47513), X64 RyuJIT AVX2
```

|                 Method |       Mean |    Error |   StdDev |     Gen0 | Allocated |
|----------------------- |-----------:|---------:|---------:|---------:|----------:|
|             NewId_1000 |   861.8 us |  5.00 us |  4.68 us | 416.9922 |   1.67 MB |
| NewId_1000_NumberValue | 2,855.3 us |  5.07 us |  4.50 us | 722.6563 |   2.89 MB |
|             Parse_1000 | 1,982.8 us | 20.11 us | 18.81 us | 640.6250 |   2.56 MB |

`NewId_1000` is method generating 1000 ids and getting `Value`.
`NewId_1000_NumberValue` is method generating 1000 ids and getting `NumberValue`.
`Parse_1000` is method parsing 1000 ids.

## Changes

- 1.1.1
    - Fix bug where `IsValid` method returns `false` for `NumberValue`

- 1.1.0
    - Fixed bug when verifying validity of value
    - Added support for `NumberValue`

- 1.0.0 
    - Initial version
