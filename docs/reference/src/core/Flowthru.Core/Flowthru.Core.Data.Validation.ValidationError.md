# <a id="Flowthru_Core_Data_Validation_ValidationError"></a> Class ValidationError

Namespace: [Flowthru.Core.Data.Validation](Flowthru.Core.Data.Validation.md)  
Assembly: Flowthru.Core.dll  

Represents a single validation error discovered during catalog entry inspection.

```csharp
public class ValidationError
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ValidationError](Flowthru.Core.Data.Validation.ValidationError.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

ValidationError provides structured information about what went wrong during
inspection, making it easier to diagnose and fix data issues.

## Constructors

### <a id="Flowthru_Core_Data_Validation_ValidationError__ctor_System_String_Flowthru_Core_Data_Validation_ValidationErrorType_System_String_System_String_"></a> ValidationError\(string, ValidationErrorType, string, string?\)

Creates a new validation error.

```csharp
public ValidationError(string catalogKey, ValidationErrorType errorType, string message, string? details = null)
```

#### Parameters

`catalogKey` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry key where the error occurred

`errorType` [ValidationErrorType](Flowthru.Core.Data.Validation.ValidationErrorType.md)

The category of error

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of the error

`details` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional additional context (file path, row number, column name, etc.)

## Properties

### <a id="Flowthru_Core_Data_Validation_ValidationError_CatalogKey"></a> CatalogKey

The catalog entry key where the error occurred.

```csharp
public string CatalogKey { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Core_Data_Validation_ValidationError_Details"></a> Details

Optional additional context about the error.

```csharp
public string? Details { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Remarks

May contain:
- File path
- Row number
- Column name
- Expected vs actual values
- Stack trace (for exceptions)

### <a id="Flowthru_Core_Data_Validation_ValidationError_ErrorType"></a> ErrorType

The category of error that occurred.

```csharp
public ValidationErrorType ErrorType { get; }
```

#### Property Value

 [ValidationErrorType](Flowthru.Core.Data.Validation.ValidationErrorType.md)

### <a id="Flowthru_Core_Data_Validation_ValidationError_Message"></a> Message

Human-readable description of the error.

```csharp
public string Message { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

