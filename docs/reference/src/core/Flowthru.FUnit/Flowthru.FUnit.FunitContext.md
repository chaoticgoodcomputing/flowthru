# <a id="Flowthru_FUnit_FunitContext"></a> Class FunitContext

Namespace: [Flowthru.FUnit](Flowthru.FUnit.md)  
Assembly: Flowthru.FUnit.dll  

Framework-agnostic base class for Flowthru step and effect tests.
Subclass this in any test framework (NUnit, xUnit, MSTest) to gain
typed step invocation, pre-flight validation, sample data helpers,
and a DI service collection scoped to the test.

```csharp
public class FunitContext : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FunitContext](Flowthru.FUnit.FunitContext.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

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
Analogous to <code>BunitContext</code> in bUnit — provides a controlled environment
in which a single unit (a step function or an effect node) can be exercised
in isolation.
</p>
<p>
<strong>Usage:</strong>
<pre><code class="lang-csharp">public class EvaluateModelStepTests : FunitContext
{
    [StepTest(typeof(EvaluateModelStep))]
    public void PerfectPredictions_ShouldReturn100PercentAccuracy()
    {
        var input = (
            Samples.Of(new PredictionRow { Class = 0 }),
            Samples.Of(new LabelRow { Setosa = 1.0 })
        );
        var result = Invoke(EvaluateModelStep.Create(), input);
        Assert.That(result.Accuracy, Is.EqualTo(1.0));
    }
}</code></pre>
</p>
<p>
<strong>DI services:</strong> Register services in <xref href="Flowthru.FUnit.FunitContext.Services" data-throw-if-not-resolved="false"></xref> before
the first call to <xref href="Flowthru.FUnit.FunitContext.ServiceProvider" data-throw-if-not-resolved="false"></xref> or any <code>Invoke</code> method.
The service collection is frozen on first access.
</p>

## Properties

### <a id="Flowthru_FUnit_FunitContext_Samples"></a> Samples

Sample data construction helpers.

```csharp
public SampleBuilder Samples { get; }
```

#### Property Value

 [SampleBuilder](Flowthru.FUnit.Samples.SampleBuilder.md)

### <a id="Flowthru_FUnit_FunitContext_ServiceProvider"></a> ServiceProvider

Lazily-built DI service provider. Freezes <xref href="Flowthru.FUnit.FunitContext.Services" data-throw-if-not-resolved="false"></xref> on first access.

```csharp
protected IServiceProvider ServiceProvider { get; }
```

#### Property Value

 [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)

### <a id="Flowthru_FUnit_FunitContext_Services"></a> Services

DI service collection for the test. Register services here before the first
call to <xref href="Flowthru.FUnit.FunitContext.ServiceProvider" data-throw-if-not-resolved="false"></xref>. Frozen after first access.

```csharp
public IServiceCollection Services { get; }
```

#### Property Value

 [IServiceCollection](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection)

## Methods

### <a id="Flowthru_FUnit_FunitContext_Dispose"></a> Dispose\(\)

Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.

```csharp
public void Dispose()
```

### <a id="Flowthru_FUnit_FunitContext_Dispose_System_Boolean_"></a> Dispose\(bool\)

Dispose pattern implementation.

```csharp
protected virtual void Dispose(bool disposing)
```

#### Parameters

`disposing` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_FUnit_FunitContext_Invoke__2_System_Func___0___1____0_"></a> Invoke<TInput, TOutput\>\(Func<TInput, TOutput\>, TInput\)

Invokes a synchronous step function with the given input and returns the output.

```csharp
protected TOutput Invoke<TInput, TOutput>(Func<TInput, TOutput> step, TInput input)
```

#### Parameters

`step` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, TOutput\>

`input` TInput

#### Returns

 TOutput

#### Type Parameters

`TInput` 

`TOutput` 

### <a id="Flowthru_FUnit_FunitContext_InvokeAsync__2_System_Func___0_System_Threading_Tasks_Task___1_____0_"></a> InvokeAsync<TInput, TOutput\>\(Func<TInput, Task<TOutput\>\>, TInput\)

Invokes an asynchronous step function with the given input and returns the output.

```csharp
protected Task<TOutput> InvokeAsync<TInput, TOutput>(Func<TInput, Task<TOutput>> step, TInput input)
```

#### Parameters

`step` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<TInput, [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

`input` TInput

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>

#### Type Parameters

`TInput` 

`TOutput` 

### <a id="Flowthru_FUnit_FunitContext_InvokeAsync__2_System_Func___0_System_Threading_CancellationToken_System_Threading_Tasks_Task___1_____0_System_Threading_CancellationToken_"></a> InvokeAsync<TInput, TOutput\>\(Func<TInput, CancellationToken, Task<TOutput\>\>, TInput, CancellationToken\)

Invokes an asynchronous cancellable step function with the given input.

```csharp
protected Task<TOutput> InvokeAsync<TInput, TOutput>(Func<TInput, CancellationToken, Task<TOutput>> step, TInput input, CancellationToken cancellationToken = default)
```

#### Parameters

`step` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<TInput, [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken), [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>\>

`input` TInput

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<TOutput\>

#### Type Parameters

`TInput` 

`TOutput` 

### <a id="Flowthru_FUnit_FunitContext_Validate_Flowthru_Core_Graph_INode_System_Threading_CancellationToken_"></a> Validate\(INode, CancellationToken\)

Runs pre-flight validation on any <xref href="Flowthru.Core.Graph.INode" data-throw-if-not-resolved="false"></xref> — items, effects, or steps.

```csharp
protected Task<ValidationResult> Validate(INode node, CancellationToken cancellationToken = default)
```

#### Parameters

`node` INode

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task\-1)<ValidationResult\>

