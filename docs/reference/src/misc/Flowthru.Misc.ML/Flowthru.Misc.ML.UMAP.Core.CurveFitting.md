# <a id="Flowthru_Misc_ML_UMAP_Core_CurveFitting"></a> Class CurveFitting

Namespace: [Flowthru.Misc.ML.UMAP.Core](Flowthru.Misc.ML.UMAP.Core.md)  
Assembly: Flowthru.Misc.ML.dll  

Helper for computing UMAP curve fitting parameters.

```csharp
public static class CurveFitting
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CurveFitting](Flowthru.Misc.ML.UMAP.Core.CurveFitting.md)

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
UMAP uses a smooth curve to approximate the attractive force between points:
</p>
<pre><code class="lang-csharp">weight(dist) = 1 / (1 + a * dist^(2b))</code></pre>
<p>
Parameters <code>a</code> and <code>b</code> are fit to match an exponential decay curve
based on the <code>spread</code> and <code>min_dist</code> hyperparameters:
</p>
<ul><li>For dist &lt; min_dist: weight = 1.0 (fully connected)</li><li>For dist ≥ min_dist: weight = exp(-(dist - min_dist) / spread)</li></ul>
<p>
Python UMAP reference: <code>find_ab_params()</code> in <code>umap_.py</code> (lines 1393-1408)
</p>
<p>
This implementation uses the Levenberg-Marquardt algorithm to match Python's
scipy.optimize.curve_fit behavior, ensuring identical parameter values.
</p>

## Methods

### <a id="Flowthru_Misc_ML_UMAP_Core_CurveFitting_FindABParams_System_Single_System_Single_"></a> FindABParams\(float, float\)

Computes curve parameters a and b from spread and min_dist using curve fitting.

```csharp
public static (float a, float b) FindABParams(float spread, float minDist)
```

#### Parameters

`spread` [float](https://learn.microsoft.com/dotnet/api/system.single)

Effective scale of embedded points.

`minDist` [float](https://learn.microsoft.com/dotnet/api/system.single)

Minimum distance between embedded points.

#### Returns

 \([float](https://learn.microsoft.com/dotnet/api/system.single) [a](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.a), [float](https://learn.microsoft.com/dotnet/api/system.single) [b](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.single,system.single\-.b)\)

Tuple of (a, b) parameters.

