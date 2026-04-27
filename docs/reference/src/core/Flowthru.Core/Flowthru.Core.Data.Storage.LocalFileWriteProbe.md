# <a id="Flowthru_Core_Data_Storage_LocalFileWriteProbe"></a> Class LocalFileWriteProbe

Namespace: [Flowthru.Core.Data.Storage](Flowthru.Core.Data.Storage.md)  
Assembly: Flowthru.Core.dll  

Shared write-access probe for local filesystem paths.

```csharp
public static class LocalFileWriteProbe
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LocalFileWriteProbe](Flowthru.Core.Data.Storage.LocalFileWriteProbe.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Used by all file-based storage adapters and media to implement
<code>InspectTarget()</code> consistently without duplication.
</p>
<p>
<strong>Semantics:</strong>
</p>
<p>
The probe intentionally does <em>not</em> require the destination directory to
already exist. All file-based <code>Save()</code> implementations call
<code>Directory.CreateDirectory()</code> at write time, so a missing directory is never
a pre-flight blocker — only a missing or inaccessible filesystem root is.
</p>
<p>
The probe walks up the directory tree until it finds the nearest ancestor that
exists, then writes and immediately deletes a zero-byte sentinel file there. A
<xref href="Flowthru.Core.Data.Validation.ValidationErrorType.WriteAccessDenied" data-throw-if-not-resolved="false"></xref> failure is returned only when:
</p>
<ul><li>No existing ancestor can be found (e.g. a nonexistent drive or mount point)</li><li>The OS refuses the write at the nearest existing ancestor</li></ul>

## Methods

### <a id="Flowthru_Core_Data_Storage_LocalFileWriteProbe_ProbeAsync_System_String_System_Threading_CancellationToken_"></a> ProbeAsync\(string, CancellationToken\)

Probes write access for the directory that <code class="paramref">filePath</code> would be
written to, walking up the tree to the nearest existing ancestor if needed.

```csharp
public static ValueTask<ValidationResult> ProbeAsync(string filePath, CancellationToken ct)
```

#### Parameters

`filePath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The intended destination file path (need not exist yet).

`ct` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Cancellation token.

#### Returns

 [ValueTask](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask\-1)<[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)\>

