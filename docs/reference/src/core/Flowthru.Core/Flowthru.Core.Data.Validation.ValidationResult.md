# <a id="Flowthru_Core_Data_Validation_ValidationResult"></a> Class ValidationResult

Namespace: [Flowthru.Core.Data.Validation](Flowthru.Core.Data.Validation.md)  
Assembly: Flowthru.Core.dll  

Represents the result of inspecting one or more catalog entries.

```csharp
public class ValidationResult
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

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
ValidationResult provides a structured way to collect and report validation errors
discovered during catalog entry inspection. It supports both single-entry and
multi-entry validation scenarios.
</p>

## Constructors

### <a id="Flowthru_Core_Data_Validation_ValidationResult__ctor"></a> ValidationResult\(\)

Creates a successful validation result with no errors.

```csharp
public ValidationResult()
```

### <a id="Flowthru_Core_Data_Validation_ValidationResult__ctor_System_Collections_Generic_IEnumerable_Flowthru_Core_Data_Validation_ValidationError__"></a> ValidationResult\(IEnumerable<ValidationError\>\)

Creates a validation result with the specified errors.

```csharp
public ValidationResult(IEnumerable<ValidationError> errors)
```

#### Parameters

`errors` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[ValidationError](Flowthru.Core.Data.Validation.ValidationError.md)\>

Collection of validation errors

## Properties

### <a id="Flowthru_Core_Data_Validation_ValidationResult_ErrorCount"></a> ErrorCount

Number of validation errors found.

```csharp
public int ErrorCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Core_Data_Validation_ValidationResult_Errors"></a> Errors

Read-only collection of all validation errors.

```csharp
public IReadOnlyList<ValidationError> Errors { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ValidationError](Flowthru.Core.Data.Validation.ValidationError.md)\>

### <a id="Flowthru_Core_Data_Validation_ValidationResult_HasErrors"></a> HasErrors

True if one or more validation errors were found.

```csharp
public bool HasErrors { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_Validation_ValidationResult_IsValid"></a> IsValid

True if no validation errors were found.

```csharp
public bool IsValid { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="Flowthru_Core_Data_Validation_ValidationResult_Failure_System_String_Flowthru_Core_Data_Validation_ValidationErrorType_System_String_System_String_"></a> Failure\(string, ValidationErrorType, string, string?\)

Creates a failed validation result with a single error.

```csharp
public static ValidationResult Failure(string catalogKey, ValidationErrorType errorType, string message, string? details = null)
```

#### Parameters

`catalogKey` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry key where the error occurred

`errorType` [ValidationErrorType](Flowthru.Core.Data.Validation.ValidationErrorType.md)

The category of error

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

Human-readable description of the error

`details` [string](https://learn.microsoft.com/dotnet/api/system.string)?

Optional additional context

#### Returns

 [ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

### <a id="Flowthru_Core_Data_Validation_ValidationResult_FromException_System_String_System_Exception_"></a> FromException\(string, Exception\)

Creates a failed validation result from an exception.

```csharp
public static ValidationResult FromException(string catalogKey, Exception exception)
```

#### Parameters

`catalogKey` [string](https://learn.microsoft.com/dotnet/api/system.string)

The catalog entry key where the error occurred

`exception` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

The exception that occurred during inspection

#### Returns

 [ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

### <a id="Flowthru_Core_Data_Validation_ValidationResult_Success"></a> Success\(\)

Creates a successful validation result.

```csharp
public static ValidationResult Success()
```

#### Returns

 [ValidationResult](Flowthru.Core.Data.Validation.ValidationResult.md)

### <a id="Flowthru_Core_Data_Validation_ValidationResult_ThrowIfInvalid"></a> ThrowIfInvalid\(\)

Throws a ValidationException if this result has errors.

```csharp
public void ThrowIfInvalid()
```

#### Exceptions

 [ValidationException](Flowthru.Core.Data.Validation.ValidationException.md)

Thrown if validation failed

### <a id="Flowthru_Core_Data_Validation_ValidationResult_ToString"></a> ToString\(\)

Returns a formatted string representation of all errors.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

