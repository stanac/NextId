This library contains source generator for [NextId identifiers](https://github.com/stanac/nextid)
boilerplate code.

Add package reference

```xml
<PackageReference Include="NextId.Gen" 
                  Version="[2.*, 3)"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false"
                  />
```

Optionally set `Version` to be fixed, e.g. `"2.1.0"`

It will inject `NextId.IdentifierAttribute`, use it on partial class:

```csharp
[Identifier(Prefix = "user", Salt = "99AAB45utg")]
public partial class UserId;
```