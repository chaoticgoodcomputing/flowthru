# <a id="Flowthru_Core_Results_RuntimeErrorClassifier"></a> Class RuntimeErrorClassifier

Namespace: [Flowthru.Core.Results](Flowthru.Core.Results.md)  
Assembly: Flowthru.Core.dll  

Classifies runtime exceptions as external/environmental or possible framework bugs
using heuristic type matching.

```csharp
public static class RuntimeErrorClassifier
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RuntimeErrorClassifier](Flowthru.Core.Results.RuntimeErrorClassifier.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Results_RuntimeErrorClassifier_Classify_System_Exception_"></a> Classify\(Exception\)

Classifies the given exception based on its type hierarchy.

```csharp
public static ErrorClassification Classify(Exception exception)
```

#### Parameters

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

#### Returns

 [ErrorClassification](Flowthru.Core.Results.ErrorClassification.md)

#### Remarks

Walks the exception type's inheritance chain and checks inner exceptions.
Any match against known external/environmental exception types produces
<xref href="Flowthru.Core.Results.ErrorClassification.ExternalError" data-throw-if-not-resolved="false"></xref>.

