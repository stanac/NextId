# NextId
Strongly-typed, K-Sortable globally unique identifier with checksum

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

## How to use it

Install [nuget package](https://www.nuget.org/packages/NextId)

```
dotnet add package NextId
```

Inherit `NextId.Identifier` class:

```csharp
public class UserId : Identifier<UserId>
{
    private const string PrefixConst = "user";
    private const string SaltConst = "99AAB45utg";
    protected override string Prefix => PrefixConst;
    protected override string Salt => SaltConst;

    public UserId() { }
    
    // constructor with time component
    public UserId(DateTimeOffset dt) : base(dt) { }

    // constructor for parsing
    private UserId(string value) : base(value) { }

    public static UserId NewId() => new();

    public static UserId Parse(string value) => new(value);

    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
    
    // you can add TryParse if needed
}
```

You can call `new UserId()` or `UserId.NewId()` for new values, 
`IsValid` for validation of existing values and `Parse` for parsing existing values.

```csharp
UserId id1 = UserId.NewId();
UserId id2 = UserId.Parse(id1.Value);

(id1 == id2).Should().BeTrue();
```

You can get string value of identifier by calling `Value` property or `ToString()` method.

Prefix will be set as identifier prefix (`user` in this case). 
Value must have less than 12 characters and can contain only ASCII letters and digits.

Salt must be less than 33 characters and should be set to random value to each type.
Once set, it must not be changed.

## Performance

- ~866,551 generated ids per second per core
- ~369,822 parsed ids per second per core

```
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.3086/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.304
  [Host]     : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.7 (7.0.723.27404), X64 RyuJIT AVX2


|     Method |     Mean |     Error |    StdDev |
|----------- |---------:|----------:|----------:|
| NewId_1000 | 1.154 ms | 0.0054 ms | 0.0050 ms |
| Parse_1000 | 2.704 ms | 0.0309 ms | 0.0258 ms |

```