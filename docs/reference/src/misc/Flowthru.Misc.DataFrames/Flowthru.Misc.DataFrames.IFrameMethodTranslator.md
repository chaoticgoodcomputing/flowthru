# <a id="Flowthru_Misc_DataFrames_IFrameMethodTranslator"></a> Interface IFrameMethodTranslator

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Translates .NET method calls into native frame operations.

```csharp
public interface IFrameMethodTranslator
```

## Remarks

Providers register implementations to teach the expression visitor how to handle
method calls — for example, translating <code>Math.Abs(x)</code> into a native
absolute-value function.

## Methods

### <a id="Flowthru_Misc_DataFrames_IFrameMethodTranslator_Translate_System_Reflection_MethodInfo_System_Object_System_Collections_Generic_IReadOnlyList_System_Object__"></a> Translate\(MethodInfo, object?, IReadOnlyList<object?\>\)

Attempts to translate a method call into a native expression.

```csharp
object? Translate(MethodInfo method, object? instance, IReadOnlyList<object?> arguments)
```

#### Parameters

`method` [MethodInfo](https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo)

The method being called.

`instance` [object](https://learn.microsoft.com/dotnet/api/system.object)?

The translated native expression for the instance, or <code>null</code> for static methods.

`arguments` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[object](https://learn.microsoft.com/dotnet/api/system.object)?\>

The translated native expressions for each argument.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

A native expression, or <code>null</code> if this translator does not handle the method.

