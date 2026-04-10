# <a id="Flowthru_Core_Steps_NoParams"></a> Class NoParams

Namespace: [Flowthru.Core.Steps](Flowthru.Core.Steps.md)  
Assembly: Flowthru.Core.dll  

Marker type for nodes that don't require parameters.
Used as the default TParameters type in StepBase&lt;TInput, TOutput, TParameters&gt;.

```csharp
public sealed class NoParams
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[NoParams](Flowthru.Core.Steps.NoParams.md)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
This is a simple empty class that serves as a shorthand for users and the library
when no parameters are needed for a node. Steps that don't need configuration can
omit the third type parameter by using the two-parameter StepBase&lt;TInput, TOutput&gt;
convenience base class.
</p>
<p>
<strong>Usage Examples:</strong>
</p>
<pre><code class="lang-csharp">// Explicit NoParams (rarely needed)
public class MyStep : StepBase&lt;Input, Output, NoParams&gt; { }

// Recommended: Use two-parameter base class
public class MyStep : StepBase&lt;Input, Output&gt; { }

// With parameters
public class ConfigurableStep : StepBase&lt;Input, Output, MyParameters&gt;
{
    // Parameters property is automatically available
}</code></pre>

