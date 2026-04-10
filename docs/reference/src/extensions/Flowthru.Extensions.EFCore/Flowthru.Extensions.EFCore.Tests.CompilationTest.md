# <a id="Flowthru_Extensions_EFCore_Tests_CompilationTest"></a> Class CompilationTest

Namespace: [Flowthru.Extensions.EFCore.Tests](Flowthru.Extensions.EFCore.Tests.md)  
Assembly: Flowthru.Extensions.EFCore.dll  

Minimal compilation test to verify extension pattern works.

```csharp
public class CompilationTest
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CompilationTest](Flowthru.Extensions.EFCore.Tests.CompilationTest.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_EFCore_Tests_CompilationTest_DbContextFactoryOverloadsWork"></a> DbContextFactoryOverloadsWork\(\)

Verifies that the IDbContextFactory overloads compile correctly, allowing for the idiomatic EFCore pattern of using IDbContextFactory for per-operation context creation. This ensures that both the factory and the optional save delegate with typed context compile as intended.

```csharp
public void DbContextFactoryOverloadsWork()
```

### <a id="Flowthru_Extensions_EFCore_Tests_CompilationTest_PartialClassExtensionWorks"></a> PartialClassExtensionWorks\(\)

Verifies that the partial class and extension method patterns compile correctly, allowing EFCoreItemFactory.Enumerable.EFCore to be used as intended.

```csharp
public void PartialClassExtensionWorks()
```

### <a id="Flowthru_Extensions_EFCore_Tests_CompilationTest_SingleTypedContextOverloadsWork"></a> SingleTypedContextOverloadsWork\(\)

Verifies that the single-entity EFCore item factory overloads compile correctly, allowing for both the typed context factory and IDbContextFactory patterns to be used with EFCoreSingleStorageAdapter. This ensures that the extension methods for single-entity storage compile and return the correct types as intended.
The single-entity storage adapter has similar overloads to the enumerable version, so this test ensures that both sets of overloads work correctly in parallel.
Note: This test focuses on compilation; runtime behavior (e.g. actual database operations) is not verified here.

```csharp
public void SingleTypedContextOverloadsWork()
```

### <a id="Flowthru_Extensions_EFCore_Tests_CompilationTest_TypedContextFactoryOverloadsWork"></a> TypedContextFactoryOverloadsWork\(\)

Verifies that the typed context factory overloads compile correctly, allowing for type-safe DbContext factories without casts in delegates. This ensures that the generic type parameters flow through the factory and delegates as intended.

```csharp
public void TypedContextFactoryOverloadsWork()
```

