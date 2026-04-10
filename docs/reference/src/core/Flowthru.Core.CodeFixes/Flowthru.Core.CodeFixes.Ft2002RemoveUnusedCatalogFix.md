# <a id="Flowthru_Core_CodeFixes_Ft2002RemoveUnusedCatalogFix"></a> Class Ft2002RemoveUnusedCatalogFix

Namespace: [Flowthru.Core.CodeFixes](Flowthru.Core.CodeFixes.md)  
Assembly: Flowthru.Core.CodeFixes.dll  

Code fix for FT2002: removes the <code>RegisterCatalog</code> call that is registered
but never referenced by any flow.

```csharp
[ExportCodeFixProvider("C#", new string[] { }, Name = "Ft2002RemoveUnusedCatalogFix")]
[Shared]
public sealed class Ft2002RemoveUnusedCatalogFix : CodeFixProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CodeFixProvider](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider) ← 
[Ft2002RemoveUnusedCatalogFix](Flowthru.Core.CodeFixes.Ft2002RemoveUnusedCatalogFix.md)

#### Inherited Members

[CodeFixProvider.RegisterCodeFixesAsync\(CodeFixContext\)](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider.registercodefixesasync), 
[CodeFixProvider.GetFixAllProvider\(\)](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider.getfixallprovider), 
[CodeFixProvider.FixableDiagnosticIds](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider.fixablediagnosticids), 
[CodeFixProvider.RequestPriority](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider.requestpriority), 
[object.Equals\(object\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object, object\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object, object\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Properties

### <a id="Flowthru_Core_CodeFixes_Ft2002RemoveUnusedCatalogFix_FixableDiagnosticIds"></a> FixableDiagnosticIds

A list of diagnostic IDs that this provider can provide fixes for.

```csharp
public override ImmutableArray<string> FixableDiagnosticIds { get; }
```

#### Property Value

 [ImmutableArray](https://learn.microsoft.com/dotnet/api/system.collections.immutable.immutablearray\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="Flowthru_Core_CodeFixes_Ft2002RemoveUnusedCatalogFix_GetFixAllProvider"></a> GetFixAllProvider\(\)

Gets an optional <xref href="Microsoft.CodeAnalysis.CodeFixes.FixAllProvider" data-throw-if-not-resolved="false"></xref> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
Return null if the provider doesn't support fix all/multiple occurrences.
Otherwise, you can return any of the well known fix all providers from <xref href="Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders" data-throw-if-not-resolved="false"></xref> or implement your own fix all provider.

```csharp
public override FixAllProvider GetFixAllProvider()
```

#### Returns

 [FixAllProvider](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.fixallprovider)

### <a id="Flowthru_Core_CodeFixes_Ft2002RemoveUnusedCatalogFix_RegisterCodeFixesAsync_Microsoft_CodeAnalysis_CodeFixes_CodeFixContext_"></a> RegisterCodeFixesAsync\(CodeFixContext\)

Computes one or more fixes for the specified <xref href="Microsoft.CodeAnalysis.CodeFixes.CodeFixContext" data-throw-if-not-resolved="false"></xref>.

```csharp
public override Task RegisterCodeFixesAsync(CodeFixContext context)
```

#### Parameters

`context` [CodeFixContext](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixcontext)

A <xref href="Microsoft.CodeAnalysis.CodeFixes.CodeFixContext" data-throw-if-not-resolved="false"></xref> containing context information about the diagnostics to fix.
The context must only contain diagnostics with a <xref href="Microsoft.CodeAnalysis.Diagnostic.Id" data-throw-if-not-resolved="false"></xref> included in the <xref href="Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider.FixableDiagnosticIds" data-throw-if-not-resolved="false"></xref> for the current provider.

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

