# <a id="Flowthru_Configuration_FlowthruConfigurationOptions"></a> Class FlowthruConfigurationOptions

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Options for configuring how Flowthru loads configuration files.

```csharp
public class FlowthruConfigurationOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

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
Flowthru uses Microsoft.Extensions.Configuration with layered configuration files.
By default, configuration is loaded in the following order (later files override earlier):
</p>
<ol><li><code>appsettings.json</code> - Base configuration (required)</li><li><code>appsettings.{Environment}.json</code> - Environment-specific overrides (optional)</li><li><code>appsettings.Local.json</code> - Local/user-specific overrides (optional, gitignored)</li></ol>
<p>
Both JSON and YAML formats are supported. YAML files follow the same pattern:
<code>appsettings.yml</code>, <code>appsettings.{Environment}.yml</code>, <code>appsettings.Local.yml</code>
</p>

## Properties

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_ConfigurationFileName"></a> ConfigurationFileName

The base filename for configuration files (without extension).

```csharp
public string ConfigurationFileName { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Defaults to "appsettings". Change this to use a different naming convention
(e.g., "parameters" to match Kedro's convention).

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_ConfigurationPath"></a> ConfigurationPath

The base path where configuration files are located.

```csharp
public string ConfigurationPath { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Defaults to the current directory. Can be set to "conf" for Kedro-style projects
or any other directory containing configuration files.

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_EnableYamlSupport"></a> EnableYamlSupport

Whether to support YAML configuration files in addition to JSON.

```csharp
public bool EnableYamlSupport { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

When enabled, Flowthru will load both .json and .yml/.yaml files.
Requires NetEscapades.Configuration.Yaml package.
Defaults to true for Kedro compatibility.

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_Environment"></a> Environment

The environment name used to load environment-specific configuration files.

```csharp
public string? Environment { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

<p>
If not explicitly set, Flowthru will attempt to resolve the environment in this order:
</p>
<ol><li>Value passed to <code>WithEnvironment()</code></li><li>Environment variable specified by <xref href="Flowthru.Configuration.FlowthruConfigurationOptions.EnvironmentVariable" data-throw-if-not-resolved="false"></xref></li><li><code>DOTNET_ENVIRONMENT</code> environment variable</li><li><code>ASPNETCORE_ENVIRONMENT</code> environment variable</li><li>"Production" (default)</li></ol>

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_EnvironmentVariable"></a> EnvironmentVariable

The name of the environment variable to check for environment name.

```csharp
public string? EnvironmentVariable { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

Defaults to "FLOWTHRU_ENV". Set to null to disable environment variable resolution.
Standard .NET environment variables (DOTNET_ENVIRONMENT, ASPNETCORE_ENVIRONMENT)
are always checked as fallbacks.

## Methods

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_WithConfigurationFileName_System_String_"></a> WithConfigurationFileName\(string\)

Sets the base filename for configuration files (without extension).

```csharp
public FlowthruConfigurationOptions WithConfigurationFileName(string fileName)
```

#### Parameters

`fileName` [string](https://learn.microsoft.com/dotnet/api/system.string)

The base filename (e.g., "parameters", "config")

#### Returns

 [FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

This options instance for fluent chaining

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_WithConfigurationPath_System_String_"></a> WithConfigurationPath\(string\)

Sets the base path where configuration files are located.

```csharp
public FlowthruConfigurationOptions WithConfigurationPath(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

The configuration directory path

#### Returns

 [FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

This options instance for fluent chaining

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_WithEnvironment_System_String_"></a> WithEnvironment\(string\)

Sets the environment name explicitly.

```csharp
public FlowthruConfigurationOptions WithEnvironment(string environment)
```

#### Parameters

`environment` [string](https://learn.microsoft.com/dotnet/api/system.string)

The environment name (e.g., "Development", "Production")

#### Returns

 [FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

This options instance for fluent chaining

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_WithEnvironmentVariable_System_String_"></a> WithEnvironmentVariable\(string?\)

Sets the environment variable name to check for environment resolution.

```csharp
public FlowthruConfigurationOptions WithEnvironmentVariable(string? variableName)
```

#### Parameters

`variableName` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The environment variable name

#### Returns

 [FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

This options instance for fluent chaining

### <a id="Flowthru_Configuration_FlowthruConfigurationOptions_WithYamlSupport_System_Boolean_"></a> WithYamlSupport\(bool\)

Enables or disables YAML configuration file support.

```csharp
public FlowthruConfigurationOptions WithYamlSupport(bool enabled = true)
```

#### Parameters

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether to enable YAML support

#### Returns

 [FlowthruConfigurationOptions](Flowthru.Configuration.FlowthruConfigurationOptions.md)

This options instance for fluent chaining

