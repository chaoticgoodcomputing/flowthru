# <a id="Flowthru_Core_Effects_FlowIO_1"></a> Struct FlowIO<A\>

Namespace: [Flowthru.Core.Effects](Flowthru.Core.Effects.md)  
Assembly: Flowthru.Core.dll  

Represents a cancellable asynchronous effect that produces a value of type <code class="typeparamref">A</code>.

```csharp
public readonly struct FlowIO<A>
```

#### Type Parameters

`A` 

The type of value produced by this effect.

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
<xref href="Flowthru.Core.Effects.FlowIO%601" data-throw-if-not-resolved="false"></xref> is a lightweight effect monad for representing I/O operations.
It provides:
</p>
<ul><li>Lazy evaluation - effects don't run until <xref href="Flowthru.Core.Effects.FlowIO%601.Run(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref> is called</li><li>Cancellation support - all effects accept a <xref href="System.Threading.CancellationToken" data-throw-if-not-resolved="false"></xref></li><li>Functor/Monad operations - <xref href="Flowthru.Core.Effects.FlowIO%601.Map%60%601(System.Func%7b%600%2c%60%600%7d)" data-throw-if-not-resolved="false"></xref>, Bind&lt;B&gt;</li><li>LINQ comprehension syntax - via <xref href="Flowthru.Core.Effects.FlowIO%601.Select%60%601(System.Func%7b%600%2c%60%600%7d)" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Effects.FlowIO%601.SelectMany%60%602(System.Func%7b%600%2cFlowthru.Core.Effects.FlowIO%7b%60%600%7d%7d%2cSystem.Func%7b%600%2c%60%600%2c%60%601%7d)" data-throw-if-not-resolved="false"></xref></li></ul>
<p>
<strong>Example - Basic usage:</strong>
</p>
<pre><code class="lang-csharp">FlowIO&lt;string&gt; ReadFile(string path) =&gt;
    FlowIO.LiftAsync(ct =&gt; File.ReadAllTextAsync(path, ct));

FlowIO&lt;int&gt; GetWordCount(string path) =&gt;
    from content in ReadFile(path)
    select content.Split(' ').Length;

int count = await GetWordCount("data.txt").Run();</code></pre>
<p>
<strong>Example - Error handling:</strong>
</p>
<pre><code class="lang-csharp">FlowIO&lt;Data&gt; LoadData() =&gt;
    FlowIO.LiftAsync(async ct =&gt; {
        if (!File.Exists("data.json"))
            throw new FileNotFoundException("Data file missing");
        return await JsonSerializer.DeserializeAsync&lt;Data&gt;(...);
    });

try {
    var data = await LoadData().Run();
}
catch (FileNotFoundException ex) {
    // Handle error
}</code></pre>

## Methods

### <a id="Flowthru_Core_Effects_FlowIO_1_Map__1_System_Func__0___0__"></a> Map<B\>\(Func<A, B\>\)

Maps the result of this effect using the specified function.

```csharp
public FlowIO<B> Map<B>(Func<A, B> f)
```

#### Parameters

`f` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<A, B\>

The function to apply to the effect's result.

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<B\>

A new effect that applies <code class="paramref">f</code> to this effect's result.

#### Type Parameters

`B` 

The result type after mapping.

#### Remarks

This is the functor's <code>fmap</code> operation. It transforms the value inside the effect
without changing the effect structure.

### <a id="Flowthru_Core_Effects_FlowIO_1_Run_System_Threading_CancellationToken_"></a> Run\(CancellationToken\)

Executes this effect and returns the result.

```csharp
public ValueTask<A> Run(CancellationToken token = default)
```

#### Parameters

`token` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

Optional cancellation token to cancel the effect.

#### Returns

 [ValueTask](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask\-1)<A\>

A <xref href="System.Threading.Tasks.ValueTask%601" data-throw-if-not-resolved="false"></xref> representing the asynchronous operation.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Thrown if the effect is uninitialized.

### <a id="Flowthru_Core_Effects_FlowIO_1_Select__1_System_Func__0___0__"></a> Select<B\>\(Func<A, B\>\)

Transforms this effect using the specified function (alias for <xref href="Flowthru.Core.Effects.FlowIO%601.Map%60%601(System.Func%7b%600%2c%60%600%7d)" data-throw-if-not-resolved="false"></xref>).
Enables LINQ <code>select</code> syntax.

```csharp
public FlowIO<B> Select<B>(Func<A, B> selector)
```

#### Parameters

`selector` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<A, B\>

The function to apply to the effect's result.

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<B\>

A new effect with the transformed result.

#### Type Parameters

`B` 

The result type after transformation.

#### Remarks

<strong>Example:</strong>
<pre><code class="lang-csharp">var result = from x in GetValue()
             select x * 2;</code></pre>

### <a id="Flowthru_Core_Effects_FlowIO_1_SelectMany__2_System_Func__0_Flowthru_Core_Effects_FlowIO___0___System_Func__0___0___1__"></a> SelectMany<B, C\>\(Func<A, FlowIO<B\>\>, Func<A, B, C\>\)

Projects this effect through a function that produces another effect, then combines
both results using a projection function. Enables LINQ <code>from...from...select</code> syntax.

```csharp
public FlowIO<C> SelectMany<B, C>(Func<A, FlowIO<B>> bind, Func<A, B, C> project)
```

#### Parameters

`bind` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<A, [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<B\>\>

The function that produces the next effect based on this effect's result.

`project` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<A, B, C\>

The function that combines both results into the final result.

#### Returns

 [FlowIO](Flowthru.Core.Effects.FlowIO\-1.md)<C\>

A new effect representing the composition.

#### Type Parameters

`B` 

The intermediate result type.

`C` 

The final result type.

#### Remarks

<strong>Example:</strong>
<pre><code class="lang-csharp">var result = from x in GetFirstValue()
             from y in GetSecondValue(x)
             select x + y;</code></pre>

