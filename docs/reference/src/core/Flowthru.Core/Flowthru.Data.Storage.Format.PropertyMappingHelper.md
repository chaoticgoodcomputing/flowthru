# <a id="Flowthru_Data_Storage_Format_PropertyMappingHelper"></a> Class PropertyMappingHelper

Namespace: [Flowthru.Data.Storage.Format](Flowthru.Data.Storage.Format.md)  
Assembly: Flowthru.Core.dll  

Helper for mapping external field names to C# property names using SerializedLabel attribute.

```csharp
public static class PropertyMappingHelper
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PropertyMappingHelper](Flowthru.Data.Storage.Format.PropertyMappingHelper.md)

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
<strong>Purpose:</strong> Centralized property mapping logic used by all format serializers.
</p>
<p>
<strong>Mapping Strategy:</strong>
</p>
<ol><li>Check for [SerializedLabel] attribute - if present, use that label</li><li>Fall back to property name if no attribute</li><li>Use case-insensitive comparison for lookups</li></ol>
<p>
<strong>Extensibility:</strong> New format serializers should use this helper to ensure
consistent behavior across all storage mechanisms.
</p>

## Methods

### <a id="Flowthru_Data_Storage_Format_PropertyMappingHelper_BuildPropertyMap__1"></a> BuildPropertyMap<T\>\(\)

Builds a mapping from external field names to PropertyInfo objects.

```csharp
public static Dictionary<string, PropertyInfo> BuildPropertyMap<T>()
```

#### Returns

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [PropertyInfo](https://learn.microsoft.com/dotnet/api/system.reflection.propertyinfo)\>

Dictionary mapping external field names (case-insensitive) to PropertyInfo

#### Type Parameters

`T` 

The schema type

#### Examples

<pre><code class="lang-csharp">// Build property map for a schema
var propertyMap = PropertyMappingHelper.BuildPropertyMap&lt;ShuttleSchema&gt;();

// Look up property by external field name
if (propertyMap.TryGetValue("shuttle_location", out var property))
{
    // Maps to ShuttleLocation property
    var value = reader.GetValue("shuttle_location");
    property.SetValue(instance, value);
}</code></pre>

#### Remarks

<p>
The returned dictionary uses case-insensitive string comparison, allowing flexible
matching of external field names regardless of casing.
</p>
<p>
<strong>Lookup Priority:</strong>
</p>
<ol><li>[SerializedLabel("field_name")] - explicit attribute takes precedence</li><li>Property.Name - fallback if no attribute present</li></ol>

### <a id="Flowthru_Data_Storage_Format_PropertyMappingHelper_GetFieldName_System_Reflection_PropertyInfo_"></a> GetFieldName\(PropertyInfo\)

Gets the external field name for a property.

```csharp
public static string GetFieldName(PropertyInfo property)
```

#### Parameters

`property` [PropertyInfo](https://learn.microsoft.com/dotnet/api/system.reflection.propertyinfo)

The property to get the field name for

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

The external field name (from SerializedLabel or property name)

#### Examples

<pre><code class="lang-csharp">var properties = typeof(ShuttleSchema).GetProperties();
foreach (var property in properties)
{
    var fieldName = PropertyMappingHelper.GetFieldName(property);
    writer.WriteField(fieldName, property.GetValue(instance));
}</code></pre>

#### Remarks

<p>
This method is useful for serialization scenarios where you need to write
property values to external storage with the correct field names.
</p>

