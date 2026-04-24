# <a id="Flowthru_Core_Results_ErrorClassification"></a> Enum ErrorClassification

Namespace: [Flowthru.Core.Results](Flowthru.Core.Results.md)  
Assembly: Flowthru.Core.dll  

Classifies a runtime failure as either an external/environmental error
or a possible framework bug.

```csharp
public enum ErrorClassification
```

## Fields

`ExternalError = 0` 

The failure appears to be caused by external factors (network, OOM, cancellation, I/O).



`PossibleFrameworkBug = 1` 

The failure does not match any known external cause and may indicate a Flowthru bug.



