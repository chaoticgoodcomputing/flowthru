# <a id="Flowthru_Core_Results_GitHubIssueUrlBuilder"></a> Class GitHubIssueUrlBuilder

Namespace: [Flowthru.Core.Results](Flowthru.Core.Results.md)  
Assembly: Flowthru.Core.dll  

Builds a pre-filled GitHub issue URL from a <xref href="Flowthru.Core.Results.RuntimeErrorReport" data-throw-if-not-resolved="false"></xref>.

```csharp
public static class GitHubIssueUrlBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GitHubIssueUrlBuilder](Flowthru.Core.Results.GitHubIssueUrlBuilder.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Results_GitHubIssueUrlBuilder_Build_Flowthru_Core_Results_RuntimeErrorReport_"></a> Build\(RuntimeErrorReport\)

Generates a GitHub new-issue URL pre-populated with failure context.

```csharp
public static string Build(RuntimeErrorReport report)
```

#### Parameters

`report` [RuntimeErrorReport](Flowthru.Core.Results.RuntimeErrorReport.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

