# NextId Library Usage Instructions for LLMs

This document provides instructions for Large Language Models (LLMs) on how to correctly generate code using the `NextId` library for .NET.

## Library Overview

**NextId** is a library for generating strongly-typed, K-Sortable globally unique identifiers with checksums for .NET 8+. 

### Key Characteristics
- **K-Sortable**: IDs are sortable by time.
- **Strongly Typed**: Each ID type is a distinct class, preventing mix-ups (e.g., passing a `UserId` where an `OrderId` is expected).
- **Checksum**: Built-in checksum to validate ID integrity before DB lookups.
- **Structure**: `{Prefix}-{TimeComponent}{RandomComponent}{Checksum}`
  - `Prefix`: User-defined (1-11 chars, ASCII alphanumeric).
  - `TimeComponent`: Microsecond precision Unix timestamp (12 chars).
  - `RandomComponent`: Random value (12 chars).
  - `Checksum`: Validation checksum (3 chars).
- **Two Representations**:
  - `Value`: The standard string format (e.g., `user-222v7NyttKhf2dvcpStpdNKD9TW`).
  - `NumberValue`: An obfuscated, numeric-only string format suitable for public URLs (e.g., `user-9646185430515823890035197343360748348694018675`).

---

## Installation

When generating code, include the following NuGet packages:

1.  **Core Package**: `NextId` (Required)
2.  **Source Generator**: `NextId.Gen` (Recommended for boilerplate)
3.  **JSON Serialization**: `NextId.Serialization.Json` (Optional, for `System.Text.Json` support)

```bash
dotnet add package NextId
dotnet add package NextId.Serialization.Json
```

For the source generator **NextId.Gen**, add it as an analyzer in your project's `.csproj` file:

```xml
<PackageReference Include="NextId.Gen" 
                  Version="[2.*, 3)"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

---

## Defining Identifiers

### Method 1: Source Generator (Recommended)

Use the `[Identifier]` attribute on a `partial` class. This mimics specific behavior and ensures optimal performance.

**Constraints:**
- `Prefix`: 1-11 characters, ASCII alphanumeric, no whitespace.
- `Salt`: Max 32 characters, no whitespace. **MUST** be a random string unique to this ID type.

```csharp
using NextId;

// Attribute triggers the source generator
[Identifier(Prefix = "user", Salt = "99AAB45utgSecretSalt")] 
public partial class UserId;
```

**What is generated:**
- `NewId()`: Creates a new ID.
- `Parse(string)`: Parses string to ID.
- `TryParse(string, out ...)`: Safe parsing.
- `IsValid(string)`: Validates format and checksum.
- Implements `IEquatable<UserId>`, `IParsable<UserId>`.

### Method 2: Manual Inheritance (Legacy/Custom)

Inherit from `Identifier<T>`. Only use this if the source generator cannot be used.

```csharp
using NextId;
using System.Diagnostics.CodeAnalysis;

public class OrderId : Identifier<OrderId>, IParsable<OrderId>
{
    private const string PrefixConst = "ord";
    private const string SaltConst = "AnotherSecretSalt123";

    protected override string Prefix => PrefixConst;
    protected override string Salt => SaltConst;

    // Required constructors
    private OrderId() { }
    public OrderId(DateTimeOffset dt) : base(dt) { }
    private OrderId(string value) : base(value) { }

    // Boilerplate factory methods
    public static OrderId NewId() => new();
    public static OrderId Parse(string s) => Parse(s, null);
    public static OrderId Parse(string s, IFormatProvider? provider) => new(s);
    public static bool TryParse(string? s, IFormatProvider? provider, [NotNullWhen(true)] out OrderId? result)
    {
        try { result = new RequestId(s!); return true; }
        catch { result = null; return false; }
    }
    public static bool IsValid(string value) => IsValid(value, PrefixConst, SaltConst);
}
```

---

## Core Usage

### Generating IDs

```csharp
var userId = UserId.NewId();
Console.WriteLine(userId.Value);       // e.g., user-222...
Console.WriteLine(userId.NumberValue); // e.g., user-964...
```

### Parsing IDs

```csharp
// From standard value
var id1 = UserId.Parse("user-222v7NyttKhf2dvcpStpdNKD9TW");

// From obfuscated number value
var id2 = UserId.Parse("user-9646185430515823890035197343360748348694018675");
```

### Validation

```csharp
if (UserId.IsValid("user-invalid-id")) 
{
    // Proceed
}
```

---

## Serialization (System.Text.Json)

To enable JSON serialization:

1.  Register converters in `Program.cs` or where `JsonSerializerOptions` are configured.
2.  Choose representation:
    - Standard `Value` (default behavior).
    - Obfuscated `NumberValue` (set `serializeIdsAsNumberValues: true`).

```csharp
using System.Text.Json;
using NextId.Serialization.Json;

var options = new JsonSerializerOptions();

// false = serialize as standard string (user-222...)
// true  = serialize as number string (user-964...)
options.AddIdentifierConverters(serializeIdsAsNumberValues: false);

var user = new User { Id = UserId.NewId() };
string json = JsonSerializer.Serialize(user, options);
```

---

## Best Practices for Generation

1.  **Unique Salts**: Always use a different, random salt for each Identifier type. This prevents ID spoofing between types and ensures security of the obfuscation.
2.  **Short Prefixes**: Keep prefixes short (e.g., 3-4 chars) to save storage space while maintaining readability.
3.  **Validation**: Always use `TryParse` or `IsValid` when accepting IDs from external sources (API endpoints, etc.) to verify checksums before database queries.
4.  **Database Storage**: Store as `varchar` (string). Calculate length index based on expected max length (approx 30 chars for standard, 50 chars for numeric).
