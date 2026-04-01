# <a id="Flowthru_Data_Validation_ValidationErrorType"></a> Enum ValidationErrorType

Namespace: [Flowthru.Data.Validation](Flowthru.Data.Validation.md)  
Assembly: Flowthru.Core.dll  

Categories of validation errors that can occur during catalog entry inspection.

```csharp
public enum ValidationErrorType
```

## Fields

`DeserializationError = 4` 

A row failed to deserialize (missing required field, invalid value, etc.).



`EmptyDataset = 5` 

The data source is empty when data was expected.



`InspectionFailure = 6` 

An unexpected exception occurred during inspection.



`InvalidFormat = 1` 

The data format is invalid or corrupted (malformed CSV, corrupt Parquet, etc.).



`NotFound = 0` 

The data source does not exist (file not found, URL unreachable, etc.).



`SchemaMismatch = 2` 

Headers or column names don't match the expected schema.



`TypeMismatch = 3` 

Data types in the source don't match the expected types.



