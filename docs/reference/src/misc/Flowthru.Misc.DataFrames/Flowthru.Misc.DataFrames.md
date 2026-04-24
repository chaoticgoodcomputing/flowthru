# <a id="Flowthru_Misc_DataFrames"></a> Namespace Flowthru.Misc.DataFrames

### Classes

 [AggregationContext<TKey, TSource\>](Flowthru.Misc.DataFrames.AggregationContext\-2.md)

Provides typed aggregate function placeholders within an
<xref href="Flowthru.Misc.DataFrames.GroupedFrameExtensions.Aggregate%60%603(Flowthru.Misc.DataFrames.GroupedFrame%7b%60%600%2c%60%601%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7bFlowthru.Misc.DataFrames.AggregationContext%7b%60%600%2c%60%601%7d%2c%60%602%7d%7d)" data-throw-if-not-resolved="false"></xref> expression.

 [FrameExpressionVisitor](Flowthru.Misc.DataFrames.FrameExpressionVisitor.md)

Base class for translating <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> expression trees into native
frame operations.

 [FrameWindowSpec<TSource\>](Flowthru.Misc.DataFrames.FrameWindowSpec\-1.md)

An immutable, framework-agnostic window specification that describes how rows are
partitioned and ordered for windowed computations.

 [GroupedFrame<TKey, TSource\>](Flowthru.Misc.DataFrames.GroupedFrame\-2.md)

An intermediate representation of a grouped <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref>, produced by
<xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.GroupBy%60%602(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7b%60%600%2c%60%601%7d%7d)" data-throw-if-not-resolved="false"></xref>.

 [GroupedFrameExtensions](Flowthru.Misc.DataFrames.GroupedFrameExtensions.md)

Extension methods for <xref href="Flowthru.Misc.DataFrames.GroupedFrame%602" data-throw-if-not-resolved="false"></xref>.

 [TypedFrame<T\>](Flowthru.Misc.DataFrames.TypedFrame\-1.md)

A phantom-typed wrapper around an untyped DataFrame-like object.

 [TypedFrameExtensions](Flowthru.Misc.DataFrames.TypedFrameExtensions.md)

LINQ-style extension methods for <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> that build expression trees.

 [WindowContext<TSource\>](Flowthru.Misc.DataFrames.WindowContext\-1.md)

A throw-only marker type whose methods are intercepted as expression tree nodes
inside a <xref href="Flowthru.Misc.DataFrames.TypedFrameExtensions.SelectOver%60%602(Flowthru.Misc.DataFrames.TypedFrame%7b%60%600%7d%2cSystem.Linq.Expressions.Expression%7bSystem.Func%7b%60%600%2cFlowthru.Misc.DataFrames.WindowContext%7b%60%600%7d%2c%60%601%7d%7d)" data-throw-if-not-resolved="false"></xref> projection.

### Interfaces

 [IFrameMemberTranslator](Flowthru.Misc.DataFrames.IFrameMemberTranslator.md)

Translates .NET member access (property or field) into a native column expression.

 [IFrameMethodTranslator](Flowthru.Misc.DataFrames.IFrameMethodTranslator.md)

Translates .NET method calls into native frame operations.

 [IFrameQueryProvider](Flowthru.Misc.DataFrames.IFrameQueryProvider.md)

A query provider that creates <xref href="Flowthru.Misc.DataFrames.TypedFrame%601" data-throw-if-not-resolved="false"></xref> instances and compiles
accumulated expression trees into native frame operations.

 [IFrameWindowSpec](Flowthru.Misc.DataFrames.IFrameWindowSpec.md)

Non-generic contract for a window specification, used by visitors to translate
window definitions without requiring the generic source type parameter.

