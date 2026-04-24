# <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor"></a> Class FrameExpressionVisitor

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

Base class for translating <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> expression trees into native
frame operations.

```csharp
public abstract class FrameExpressionVisitor
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FrameExpressionVisitor](Flowthru.Misc.DataFrames.FrameExpressionVisitor.md)

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
This class mirrors the role of EF Core's <code>QueryableMethodTranslatingExpressionVisitor</code>.
It walks the expression tree built by <xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions" data-throw-if-not-resolved="false"></xref> methods and
dispatches to abstract handler methods that providers implement for their native backend.
</p>
<p>
Providers subclass this and implement the <code>Translate*</code> methods to emit native
operations (e.g., Spark <code>Column</code> expressions, ML.NET <code>IEstimator</code> chains).
</p>

## Methods

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_CompileExpression_System_Linq_Expressions_Expression_"></a> CompileExpression\(Expression\)

Compiles a full expression tree into a native frame object.

```csharp
public object CompileExpression(Expression expression)
```

#### Parameters

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Remarks

The expression tree is rooted at a <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> constant (a leaf
backed by a native frame), with chained <xref href="System.Linq.Expressions.MethodCallExpression" data-throw-if-not-resolved="false"></xref> nodes
representing operations like Where, Select, and Join.

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_EvaluateConstant_System_Linq_Expressions_Expression_"></a> EvaluateConstant\(Expression\)

Evaluates an expression that doesn't reference any DataFrame columns
(e.g., a closure-captured variable or a static field) to its runtime value.

```csharp
protected static object? EvaluateConstant(Expression expression)
```

#### Parameters

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)?

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_ResolveColumnName_System_Reflection_MemberInfo_"></a> ResolveColumnName\(MemberInfo\)

Resolves the external column name for a schema property, respecting
<xref href="Flowthru.Core.Abstractions.SerializedLabelAttribute" data-throw-if-not-resolved="false"></xref> if present.

```csharp
protected static string ResolveColumnName(MemberInfo member)
```

#### Parameters

`member` [MemberInfo](https://learn.microsoft.com/dotnet/api/system.reflection.memberinfo)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateAggregate_System_Linq_Expressions_MethodCallExpression_"></a> TranslateAggregate\(MethodCallExpression\)

Translates an <code>Aggregate</code> operation on a grouped frame into a native aggregation.

```csharp
protected abstract object TranslateAggregate(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] grouped source expression, [1] quoted result selector.
The result selector's body is a <code>MemberInitExpression</code> or <code>NewExpression</code>
whose bindings reference <xref href="Flowthru.Misc.DataFrames.AggregationContext%602" data-throw-if-not-resolved="false"></xref> methods.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateConstant_System_Linq_Expressions_ConstantExpression_"></a> TranslateConstant\(ConstantExpression\)

Translates a <xref href="System.Linq.Expressions.ConstantExpression" data-throw-if-not-resolved="false"></xref> wrapping a root <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref>
into the native frame it represents.

```csharp
protected abstract object TranslateConstant(ConstantExpression node)
```

#### Parameters

`node` [ConstantExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.constantexpression)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateCount_System_Linq_Expressions_MethodCallExpression_"></a> TranslateCount\(MethodCallExpression\)

Translates a <code>Count</code> operation into a scalar row count.

```csharp
protected abstract object TranslateCount(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateDistinct_System_Linq_Expressions_MethodCallExpression_"></a> TranslateDistinct\(MethodCallExpression\)

Translates a <code>Distinct</code> operation into a native deduplication.

```csharp
protected abstract object TranslateDistinct(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateGroupBy_System_Linq_Expressions_MethodCallExpression_"></a> TranslateGroupBy\(MethodCallExpression\)

Translates a <code>GroupBy</code> operation into a native grouped dataset.

```csharp
protected abstract object TranslateGroupBy(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source, [1] quoted key selector.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateJoin_System_Linq_Expressions_MethodCallExpression_"></a> TranslateJoin\(MethodCallExpression\)

Translates a <code>Join</code> operation into a native join + projection.

```csharp
protected abstract object TranslateJoin(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] outer source, [1] inner source,
[2] outer key selector, [3] inner key selector, [4] result selector.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateMethodCall_System_Linq_Expressions_MethodCallExpression_"></a> TranslateMethodCall\(MethodCallExpression\)

Dispatches a method call to the appropriate handler, or throws if unrecognized.

```csharp
protected virtual object TranslateMethodCall(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateOrderBy_System_Linq_Expressions_MethodCallExpression_System_Boolean_"></a> TranslateOrderBy\(MethodCallExpression, bool\)

Translates an <code>OrderBy</code> or <code>OrderByDescending</code> operation into a native sort.

```csharp
protected abstract object TranslateOrderBy(MethodCallExpression node, bool descending)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source, [1] quoted key selector.

`descending` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

True for descending order; false for ascending.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateSelect_System_Linq_Expressions_MethodCallExpression_"></a> TranslateSelect\(MethodCallExpression\)

Translates a <code>Select</code> operation into a native projection.

```csharp
protected abstract object TranslateSelect(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source expression, [1] quoted selector lambda.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateSelectOver_System_Linq_Expressions_MethodCallExpression_"></a> TranslateSelectOver\(MethodCallExpression\)

Translates a <code>SelectOver</code> operation into a native windowed projection.

```csharp
protected abstract object TranslateSelectOver(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source expression,
[1] quoted two-parameter selector <code>(TSource, WindowContext&lt;TSource&gt;) =&gt; TResult</code>.
Bindings referencing the <code>WindowContext</code> parameter carry
<xref href="Flowthru.Misc.DataFrames.FrameWindowSpec%601" data-throw-if-not-resolved="false"></xref> arguments that describe the window.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateTake_System_Linq_Expressions_MethodCallExpression_"></a> TranslateTake\(MethodCallExpression\)

Translates a <code>Take</code> operation into a native row limit.

```csharp
protected abstract object TranslateTake(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source, [1] count constant.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateUnion_System_Linq_Expressions_MethodCallExpression_"></a> TranslateUnion\(MethodCallExpression\)

Translates a <code>Union</code> operation into a native row-wise concatenation.

```csharp
protected abstract object TranslateUnion(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source, [1] other source.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_TranslateWhere_System_Linq_Expressions_MethodCallExpression_"></a> TranslateWhere\(MethodCallExpression\)

Translates a <code>Where</code> operation into a native filter.

```csharp
protected abstract object TranslateWhere(MethodCallExpression node)
```

#### Parameters

`node` [MethodCallExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.methodcallexpression)

Method call with arguments: [0] source expression, [1] quoted predicate lambda.

#### Returns

 [object](https://learn.microsoft.com/dotnet/api/system.object)

### <a id="Flowthru_Misc_DataFrames_FrameExpressionVisitor_Unquote_System_Linq_Expressions_Expression_"></a> Unquote\(Expression\)

Extracts the <xref href="System.Linq.Expressions.LambdaExpression" data-throw-if-not-resolved="false"></xref> from a quoted argument.

```csharp
protected static LambdaExpression Unquote(Expression expression)
```

#### Parameters

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

#### Returns

 [LambdaExpression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.lambdaexpression)

