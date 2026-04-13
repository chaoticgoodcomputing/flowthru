# <a id="Flowthru_Extensions_GQL_Tests_CompilationTest"></a> Class CompilationTest

Namespace: [Flowthru.Extensions.GQL.Tests](Flowthru.Extensions.GQL.Tests.md)  
Assembly: Flowthru.Extensions.GQL.dll  

Minimal compilation tests verifying all GqlItemFactory overloads and generic constraints
compile correctly. No runtime assertions — if this file compiles, the API surface is valid.

```csharp
public class CompilationTest
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CompilationTest](Flowthru.Extensions.GQL.Tests.CompilationTest.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_AllowEmptyDataParameterCompiles"></a> AllowEmptyDataParameterCompiles\(\)

Verifies that allowEmptyData propagates through each factory overload without
causing a compilation error.

```csharp
public void AllowEmptyDataParameterCompiles()
```

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_EnumerableQueryCompiles"></a> EnumerableQueryCompiles\(\)

Verifies that the non-paginated collection query overload compiles and returns the correct type.

```csharp
public void EnumerableQueryCompiles()
```

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_OffsetPagedQueryCompiles"></a> OffsetPagedQueryCompiles\(\)

Verifies that the offset-paginated overload compiles and that the pagination
strategy generic parameters flow correctly.

```csharp
public void OffsetPagedQueryCompiles()
```

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_RelayPagedQueryCompiles"></a> RelayPagedQueryCompiles\(\)

Verifies that the Relay cursor-paginated overload compiles, that the pagination
strategy generic parameters flow correctly, and that the return type is correct.

```csharp
public void RelayPagedQueryCompiles()
```

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_SingleQueryReadOnlyCompiles"></a> SingleQueryReadOnlyCompiles\(\)

Verifies that the read-only single-item query overload compiles and returns the correct type.

```csharp
public void SingleQueryReadOnlyCompiles()
```

### <a id="Flowthru_Extensions_GQL_Tests_CompilationTest_SingleQueryWithMutationCompiles"></a> SingleQueryWithMutationCompiles\(\)

Verifies that the read-write single-item query overload (with mutation delegate) compiles
and that the entry traits reflect CanWrite correctly.

```csharp
public void SingleQueryWithMutationCompiles()
```

