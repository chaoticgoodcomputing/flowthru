# <a id="Flowthru_Core_Flows_ValidationDepth"></a> Enum ValidationDepth

Namespace: [Flowthru.Core.Flows](Flowthru.Core.Flows.md)  
Assembly: Flowthru.Core.dll  

Controls how deeply a dry run validates the pipeline before stopping.

```csharp
public enum ValidationDepth
```

## Fields

`Full = 1` 

Structure validation plus external data presence checks (default dry-run behaviour).



`StructureOnly = 0` 

Validates graph structure only: no cycles, all node type contracts satisfied,
all catalog entry dependencies wired, and all validation hooks run.
No data source access.



