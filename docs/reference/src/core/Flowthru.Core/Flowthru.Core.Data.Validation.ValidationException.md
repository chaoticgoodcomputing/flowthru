# <a id="Flowthru_Core_Data_Validation_ValidationException"></a> Class ValidationException

Namespace: [Flowthru.Core.Data.Validation](Flowthru.Core.Data.Validation.md)  
Assembly: Flowthru.Core.dll  

Exception thrown when catalog entry validation fails.

```csharp
public class ValidationException : Exception, ISerializable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Exception](https://learn.microsoft.com/dotnet/api/system.exception) ← 
[ValidationException](Flowthru.Core.Data.Validation.ValidationException.md)

#### Implements

[ISerializable](https://learn.microsoft.com/dotnet/api/system.runtime.serialization.iserializable)

#### Inherited Members

[Exception.GetBaseException\(\)](https://learn.microsoft.com/dotnet/api/system.exception.getbaseexception), 
[Exception.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.exception.gettype), 
[Exception.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.exception.tostring), 
[Exception.Data](https://learn.microsoft.com/dotnet/api/system.exception.data), 
[Exception.HelpLink](https://learn.microsoft.com/dotnet/api/system.exception.helplink), 
[Exception.HResult](https://learn.microsoft.com/dotnet/api/system.exception.hresult), 
[Exception.InnerException](https://learn.microsoft.com/dotnet/api/system.exception.innerexception), 
[Exception.Message](https://learn.microsoft.com/dotnet/api/system.exception.message), 
[Exception.Source](https://learn.microsoft.com/dotnet/api/system.exception.source), 
[Exception.StackTrace](https://learn.microsoft.com/dotnet/api/system.exception.stacktrace), 
[Exception.TargetSite](https://learn.microsoft.com/dotnet/api/system.exception.targetsite), 
[Exception.SerializeObjectState](https://learn.microsoft.com/dotnet/api/system.exception.serializeobjectstate), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

This exception is thrown by <xref href="Flowthru.Core.Data.Validation.ValidationResult.ThrowIfInvalid" data-throw-if-not-resolved="false"></xref> to halt
pipeline execution when external data validation fails.

## Constructors

### <a id="Flowthru_Core_Data_Validation_ValidationException__ctor_Flowthru_Core_Data_Validation_ValidationResult_"></a> ValidationException\(ValidationResult\)

Creates a new validation exception.

```csharp
public ValidationException(ValidationResult validationResult)
```

#### Parameters

`validationResult` [ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

The validation result containing errors

## Properties

### <a id="Flowthru_Core_Data_Validation_ValidationException_ValidationResult"></a> ValidationResult

The validation result containing all errors.

```csharp
public ValidationResult ValidationResult { get; }
```

#### Property Value

 [ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

