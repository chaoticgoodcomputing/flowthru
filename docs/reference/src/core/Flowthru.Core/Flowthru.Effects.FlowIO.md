# <a id="Flowthru_Effects_FlowIO"></a> Class FlowIO

Namespace: [Flowthru.Effects](Flowthru.Effects.md)  
Assembly: Flowthru.Core.dll  

Provides factory methods and combinators for creating <xref href="Flowthru.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> effects.

```csharp
public static class FlowIO
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FlowIO](Flowthru.Effects.FlowIO.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Methods

### <a id="Flowthru_Effects_FlowIO_Fail__1_System_Exception_"></a> Fail<A\>\(Exception\)

Creates an effect that immediately fails with the given exception.

```csharp
public static FlowIO<A> Fail<A>(Exception error)
```

#### Parameters

`error` [Exception](https://learn.microsoft.com/dotnet/api/system.exception)

The exception to throw.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>

An effect that fails with <code class="paramref">error</code>.

#### Type Parameters

`A` 

The expected result type.

### <a id="Flowthru_Effects_FlowIO_Fail__1_System_String_"></a> Fail<A\>\(string\)

Creates an effect that immediately fails with an exception containing the given message.

```csharp
public static FlowIO<A> Fail<A>(string message)
```

#### Parameters

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

The error message.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>

An effect that fails with an exception containing <code class="paramref">message</code>.

#### Type Parameters

`A` 

The expected result type.

### <a id="Flowthru_Effects_FlowIO_Lift__1_System_Func___0__"></a> Lift<A\>\(Func<A\>\)

Lifts a synchronous function into an effect.

```csharp
public static FlowIO<A> Lift<A>(Func<A> f)
```

#### Parameters

`f` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<A\>

The function to lift.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>

An effect that executes <code class="paramref">f</code>.

#### Type Parameters

`A` 

The return type.

#### Remarks

The function is still executed lazily - only when <xref href="Flowthru.Effects.FlowIO%601.Run(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> is called.

### <a id="Flowthru_Effects_FlowIO_LiftAsync__1_System_Func_System_Threading_CancellationToken_System_Threading_Tasks_ValueTask___0___"></a> LiftAsync<A\>\(Func<CancellationToken, ValueTask<A\>\>\)

Lifts a cancellation-aware <xref href="System.Threading.Tasks.ValueTask%601" data-throw-if-not-resolved="false"></xref>-returning function into an effect.

```csharp
public static FlowIO<A> LiftAsync<A>(Func<CancellationToken, ValueTask<A>> f)
```

#### Parameters

`f` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [ValueTask](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask\-1)<A\>\>

The function that accepts a cancellation token and returns a <xref href="System.Threading.Tasks.ValueTask%601" data-throw-if-not-resolved="false"></xref>.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>

An effect that executes <code class="paramref">f</code>.

#### Type Parameters

`A` 

The return type.

#### Remarks

<p>
All async I/O operations should observe the cancellation token to support graceful shutdown.
If your operation is truly synchronous, use <xref href="Flowthru.Effects.FlowIO.Lift%60%601(System.Func%7b%60%600%7d)" data-throw-if-not-resolved="false"></xref> instead.
</p>
<p>
For Task-based APIs, convert using <code>.AsTask()</code>: <code>LiftAsync(async ct =&gt; await SomeTaskAsync(ct).AsTask())</code>
or rely on implicit conversion from Task to ValueTask.
</p>

### <a id="Flowthru_Effects_FlowIO_Pure__1___0_"></a> Pure<A\>\(A\)

Creates an effect that immediately returns the given value.

```csharp
public static FlowIO<A> Pure<A>(A value)
```

#### Parameters

`value` A

The value to return.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>

An effect that produces <code class="paramref">value</code>.

#### Type Parameters

`A` 

The type of value.

#### Remarks

This is the monad's <code>return</code> or <code>pure</code> operation.

### <a id="Flowthru_Effects_FlowIO_Sequence__1_System_Collections_Generic_IEnumerable_Flowthru_Effects_FlowIO___0___"></a> Sequence<A\>\(IEnumerable<FlowIO<A\>\>\)

Sequences a collection of effects, running them all and collecting the results.

```csharp
public static FlowIO<IEnumerable<A>> Sequence<A>(IEnumerable<FlowIO<A>> effects)
```

#### Parameters

`effects` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[FlowIO](Flowthru.Effects.FlowIO\-1.md)<A\>\>

The collection of effects to sequence.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<A\>\>

An effect that produces an enumerable of all results.

#### Type Parameters

`A` 

The result type of each effect.

#### Remarks

Effects are executed sequentially in the order they appear in the collection.
If any effect fails, the entire sequence fails.

### <a id="Flowthru_Effects_FlowIO_Traverse__2_System_Collections_Generic_IEnumerable___0__System_Func___0_Flowthru_Effects_FlowIO___1___"></a> Traverse<A, B\>\(IEnumerable<A\>, Func<A, FlowIO<B\>\>\)

Traverses a collection, applying an effect-producing function to each element
and collecting the results.

```csharp
public static FlowIO<IEnumerable<B>> Traverse<A, B>(IEnumerable<A> source, Func<A, FlowIO<B>> f)
```

#### Parameters

`source` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<A\>

The collection to traverse.

`f` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<A, [FlowIO](Flowthru.Effects.FlowIO\-1.md)<B\>\>

The function that produces an effect for each element.

#### Returns

 [FlowIO](Flowthru.Effects.FlowIO\-1.md)<[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<B\>\>

An effect that produces an enumerable of results.

#### Type Parameters

`A` 

The source element type.

`B` 

The result type.

