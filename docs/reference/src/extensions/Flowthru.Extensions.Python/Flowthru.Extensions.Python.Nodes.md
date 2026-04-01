# <a id="Flowthru_Extensions_Python_Nodes"></a> Namespace Flowthru.Extensions.Python.Nodes

### Classes

 [PythonNodeFactory](Flowthru.Extensions.Python.Nodes.PythonNodeFactory.md)

Extension methods for adding Python nodes to pipelines.

 [PythonNodeWrapper<TInput, TOutput\>](Flowthru.Extensions.Python.Nodes.PythonNodeWrapper\-2.md)

Thin wrapper that binds an <xref href="Flowthru.Extensions.Python.Execution.IPythonExecutor" data-throw-if-not-resolved="false"></xref> to a specific module/function pair,
exposing it as a typed <code>Func&lt;TInput, TOutput&gt;</code> for use with the pipeline builder.

