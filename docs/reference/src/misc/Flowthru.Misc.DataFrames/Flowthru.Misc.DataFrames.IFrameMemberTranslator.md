# <a id="Flowthru_Misc_DataFrames_IFrameMemberTranslator"></a> Interface IFrameMemberTranslator

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Translates .NET member access (property or field) into a native column expression.

```csharp
public interface IFrameMemberTranslator
```

## Remarks

Providers register implementations to teach the expression visitor how to handle
property reads beyond direct schema properties — for example, translating
<code>string.Length</code> into a native string-length function.

## Methods

### <a id="Flowthru_Misc_DataFrames_IFrameMemberTranslator_Translate_System_Reflection_MemberInfo_System_Object_"></a> Translate\(MemberInfo, object?\)

Attempts to translate a member access into a native expression.

```csharp
object? Translate(MemberInfo member, object? instance)
```

#### Parameters

`member` [MemberInfo](https://learn.microsoft.com/dotnet/api/system.reflection.memberinfo)

The property or field being accessed.

`instance` [object](https://learn.microsoft.com/dotnet/api/system.object)?

The translated native expression for the instance, or <code>null</code> for static members.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

A native expression, or <code>null</code> if this translator does not handle the member.

