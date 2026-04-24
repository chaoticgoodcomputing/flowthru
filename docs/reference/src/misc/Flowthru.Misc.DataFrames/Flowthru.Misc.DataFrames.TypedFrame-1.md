# <a id="Flowthru_Misc_DataFrames_TypedFrame_1"></a> Class TypedFrame<T\>

Namespace: [Flowthru.Misc.DataFrames](Flowthru.Misc.DataFrames.md)  
Assembly: Flowthru.Misc.DataFrames.dll  

A phantom-typed wrapper around an untyped DataFrame-like object.

```csharp
public class TypedFrame<T> : IOrderedQueryable<T>, IOrderedQueryable, IQueryable<T>, IEnumerable<T>, IQueryable, IEnumerable
```

#### Type Parameters

`T` 

The schema type representing the row structure. Must be annotated with
<code>[FlowthruSchema]</code> to participate in compile-time and pre-flight validation.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TypedFrame<T\>](Flowthru.Misc.DataFrames.TypedFrame\-1.md)

#### Implements

[IOrderedQueryable<T\>](https://learn.microsoft.com/dotnet/api/system.linq.iorderedqueryable\-1), 
[IOrderedQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iorderedqueryable), 
[IQueryable<T\>](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable\-1), 
[IEnumerable<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IQueryable](https://learn.microsoft.com/dotnet/api/system.linq.iqueryable), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

#### Extension Methods

[TypedFrameExtensions.Count<T\>\(TypedFrame<T\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Count\_\_1\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_), 
[TypedFrameExtensions.Distinct<T\>\(TypedFrame<T\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Distinct\_\_1\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_), 
[TypedFrameExtensions.GroupBy<T, TKey\>\(TypedFrame<T\>, Expression<Func<T, TKey\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_GroupBy\_\_2\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_1\_\_\_), 
[TypedFrameExtensions.Join<T, TInner, TKey, TResult\>\(TypedFrame<T\>, TypedFrame<TInner\>, Expression<Func<T, TKey\>\>, Expression<Func<TInner, TKey\>\>, Expression<Func<T, TInner, TResult\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Join\_\_4\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_1\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_2\_\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_1\_\_\_2\_\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_1\_\_\_3\_\_\_), 
[TypedFrameExtensions.OrderBy<T, TKey\>\(TypedFrame<T\>, Expression<Func<T, TKey\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_OrderBy\_\_2\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_1\_\_\_), 
[TypedFrameExtensions.OrderByDescending<T, TKey\>\(TypedFrame<T\>, Expression<Func<T, TKey\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_OrderByDescending\_\_2\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_1\_\_\_), 
[TypedFrameExtensions.Select<T, TResult\>\(TypedFrame<T\>, Expression<Func<T, TResult\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Select\_\_2\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_\_\_1\_\_\_), 
[TypedFrameExtensions.SelectOver<T, TResult\>\(TypedFrame<T\>, Expression<Func<T, WindowContext<T\>, TResult\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_SelectOver\_\_2\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_Flowthru\_Misc\_DataFrames\_WindowContext\_\_\_0\_\_\_\_1\_\_\_), 
[TypedFrameExtensions.Take<T\>\(TypedFrame<T\>, int\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Take\_\_1\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Int32\_), 
[TypedFrameExtensions.Union<T\>\(TypedFrame<T\>, TypedFrame<T\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Union\_\_1\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_), 
[TypedFrameExtensions.Where<T\>\(TypedFrame<T\>, Expression<Func<T, bool\>\>\)](Flowthru.Misc.DataFrames.TypedFrameExtensions.md\#Flowthru\_Misc\_DataFrames\_TypedFrameExtensions\_Where\_\_1\_Flowthru\_Misc\_DataFrames\_TypedFrame\_\_\_0\_\_System\_Linq\_Expressions\_Expression\_System\_Func\_\_\_0\_System\_Boolean\_\_\_)

## Remarks

<p>
<code>TypedFrame&lt;T&gt;</code> implements <xref href="System.Linq.IQueryable%601" data-throw-if-not-resolved="false"></xref> to leverage the standard
.NET expression tree infrastructure. The type parameter <code class="typeparamref">T</code> is a
phantom type — it carries schema information through the type system without being
instantiated at runtime.
</p>
<p>
Extension methods build expression trees via <xref href="System.Linq.IQueryProvider.CreateQuery%60%601(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref>,
threading type parameters through each operation (just as LINQ's <code>Queryable</code> methods do).
When the accumulated expression tree is compiled by the provider, it produces native
DataFrame operations (e.g., Spark Column expressions, ML.NET transforms) without
materializing data into .NET objects.
</p>

## Constructors

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1__ctor_Flowthru_Misc_DataFrames_IFrameQueryProvider_"></a> TypedFrame\(IFrameQueryProvider\)

Creates a root frame node backed by a native DataFrame.
The provider associates the native frame externally.

```csharp
public TypedFrame(IFrameQueryProvider provider)
```

#### Parameters

`provider` [IFrameQueryProvider](Flowthru.Misc.DataFrames.IFrameQueryProvider.md)

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1__ctor_Flowthru_Misc_DataFrames_IFrameQueryProvider_System_Linq_Expressions_Expression_"></a> TypedFrame\(IFrameQueryProvider, Expression\)

Creates an intermediate frame node representing an accumulated operation.
Used by the provider's <xref href="System.Linq.IQueryProvider.CreateQuery%60%601(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref>.

```csharp
public TypedFrame(IFrameQueryProvider provider, Expression expression)
```

#### Parameters

`provider` [IFrameQueryProvider](Flowthru.Misc.DataFrames.IFrameQueryProvider.md)

`expression` [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

## Properties

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1_ElementType"></a> ElementType

Gets the type of the element(s) that are returned when the expression tree associated with this instance of <xref href="System.Linq.IQueryable" data-throw-if-not-resolved="false"></xref> is executed.

```csharp
public Type ElementType { get; }
```

#### Property Value

 [Type](https://learn.microsoft.com/dotnet/api/system.type)

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1_Expression"></a> Expression

Gets the expression tree that is associated with the instance of <xref href="System.Linq.IQueryable" data-throw-if-not-resolved="false"></xref>.

```csharp
public Expression Expression { get; }
```

#### Property Value

 [Expression](https://learn.microsoft.com/dotnet/api/system.linq.expressions.expression)

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1_Provider"></a> Provider

Gets the query provider that is associated with this data source.

```csharp
public IQueryProvider Provider { get; }
```

#### Property Value

 [IQueryProvider](https://learn.microsoft.com/dotnet/api/system.linq.iqueryprovider)

## Methods

### <a id="Flowthru_Misc_DataFrames_TypedFrame_1_GetEnumerator"></a> GetEnumerator\(\)

Materializes this frame by delegating to the provider's
<xref href="Flowthru.Misc.DataFrames.IFrameQueryProvider.Materialize%60%601(System.Linq.Expressions.Expression)" data-throw-if-not-resolved="false"></xref> method.

```csharp
public IEnumerator<T> GetEnumerator()
```

#### Returns

 [IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1)<T\>

#### Remarks

This enables transparent TypedFrame → IEnumerable conversion at catalog item
boundaries: a step returning <code>TypedFrame&lt;T&gt;</code> can be wired to a
catalog item typed as <code>IEnumerable&lt;T&gt;</code> without any explicit
materialization call in step code.

