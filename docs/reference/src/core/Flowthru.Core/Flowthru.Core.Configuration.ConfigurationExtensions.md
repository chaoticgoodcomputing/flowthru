# <a id="Flowthru_Core_Configuration_ConfigurationExtensions"></a> Class ConfigurationExtensions

Namespace: [Flowthru.Core.Configuration](Flowthru.Core.Configuration.md)  
Assembly: Flowthru.Core.dll  

Extension methods for configuration-related operations.

```csharp
public static class ConfigurationExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ConfigurationExtensions](Flowthru.Core.Configuration.ConfigurationExtensions.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Core_Configuration_ConfigurationExtensions_GetValidated__1_Microsoft_Extensions_Configuration_IConfiguration_System_String_"></a> GetValidated<T\>\(IConfiguration, string\)

Binds a configuration section to a strongly-typed object and validates it.

```csharp
public static T GetValidated<T>(this IConfiguration configuration, string sectionPath) where T : new()
```

#### Parameters

`configuration` [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

The configuration instance

`sectionPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The configuration section path (e.g., "DataScience:ModelParams")

#### Returns

 T

The bound and validated object

#### Type Parameters

`T` 

The type to bind to

#### Exceptions

 [ValidationException](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationexception)

Thrown if DataAnnotations validation fails

### <a id="Flowthru_Core_Configuration_ConfigurationExtensions_GetValidatedOrDefault__1_Microsoft_Extensions_Configuration_IConfiguration_System_String_"></a> GetValidatedOrDefault<T\>\(IConfiguration, string\)

Attempts to bind and validate a configuration section, returning null if not found.

```csharp
public static T? GetValidatedOrDefault<T>(this IConfiguration configuration, string sectionPath) where T : class, new()
```

#### Parameters

`configuration` [IConfiguration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.iconfiguration)

The configuration instance

`sectionPath` [string](https://learn.microsoft.com/dotnet/api/system.string)

The configuration section path

#### Returns

 T?

The bound and validated object, or null if section doesn't exist

#### Type Parameters

`T` 

The type to bind to

#### Exceptions

 [ValidationException](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationexception)

Thrown if DataAnnotations validation fails

