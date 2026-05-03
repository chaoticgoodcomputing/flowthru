# <a id="Flowthru_FUnit_CodeFixes_Fu001ScaffoldTestsClassFix"></a> Class Fu001ScaffoldTestsClassFix

Namespace: [Flowthru.FUnit.CodeFixes](Flowthru.FUnit.CodeFixes.md)  
Assembly: Flowthru.FUnit.CodeFixes.dll  

Code fix for FU001: scaffolds a stub <code>Tests : FUnitContext</code> class inside a
<code>#if FUNIT_ENABLED</code> / <code>#endif</code> block at the end of the step class body.

```csharp
[ExportCodeFixProvider("C#", new string[] { }, Name = "Fu001ScaffoldTestsClassFix")]
[Shared]
public sealed class Fu001ScaffoldTestsClassFix : CodeFixProvider
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CodeFixProvider](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.codefixprovider) ← 
[Fu001ScaffoldTestsClassFix](Flowthru.FUnit.CodeFixes.Fu001ScaffoldTestsClassFix.md)

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

### <a id="Flowthru_FUnit_CodeFixes_Fu001ScaffoldTestsClassFix_FixableDiagnosticIds"></a> FixableDiagnosticIds

A list of diagnostic IDs that this provider can provide fixes for.

```csharp
public override ImmutableArray<string> FixableDiagnosticIds { get; }
```

#### Property Value

 [ImmutableArray](https://learn.microsoft.com/dotnet/api/system.collections.immutable.immutablearray\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="Flowthru_FUnit_CodeFixes_Fu001ScaffoldTestsClassFix_GetFixAllProvider"></a> GetFixAllProvider\(\)

Gets an optional <xref href="Microsoft.CodeAnalysis.CodeFixes.FixAllProvider" data-throw-if-not-resolved="false"></xref> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
Return null if the provider doesn't support fix all/multiple occurrences.
Otherwise, you can return any of the well known fix all providers from <xref href="Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders" data-throw-if-not-resolved="false"></xref> or implement your own fix all provider.

```csharp
public override FixAllProvider GetFixAllProvider()
```

#### Returns

 [FixAllProvider](https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.codefixes.fixallprovider)

### <a id="Flowthru_FUnit_CodeFixes_Fu001ScaffoldTestsClassFix_RegisterCodeFixesAsync_Microsoft_CodeAnalysis_CodeFixes_CodeFixContext_"></a> RegisterCodeFixesAsync\(CodeFixContext\)

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

