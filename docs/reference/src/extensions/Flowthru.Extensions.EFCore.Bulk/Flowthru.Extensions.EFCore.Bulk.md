# <a id="Flowthru_Extensions_EFCore_Bulk"></a> Namespace Flowthru.Extensions.EFCore.Bulk

### Classes

 [BulkSave](Flowthru.Extensions.EFCore.Bulk.BulkSave.md)

Factory methods that produce <code>saveFunc</code> delegates for use with
<code>EFCoreItemFactory.Enumerable.EFCore</code>. Each method returns a
<code>Func&lt;TContext, IEnumerable&lt;T&gt;, CancellationToken, Task&gt;</code>
compatible with the existing catalog item factory signature.

 [BulkSaveOptions](Flowthru.Extensions.EFCore.Bulk.BulkSaveOptions.md)

Configuration options for bulk save operations. Exposes the subset of
<code>EFCore.BulkExtensions.BulkConfig</code> properties that are relevant to
Flowthru catalog item save strategies.

 [BulkSink](Flowthru.Extensions.EFCore.Bulk.BulkSink.md)

Factory methods that produce streaming <xref href="Flowthru.Prelude.IFlowSink%601" data-throw-if-not-resolved="false"></xref> writers for
the <code>EFCore.Bulk</code> extension — the sink counterpart to the eager
<xref href="Flowthru.Extensions.EFCore.Bulk.BulkSave" data-throw-if-not-resolved="false"></xref> delegates. A sink is the terminal of a
<xref href="Flowthru.Prelude.FlowSource%601" data-throw-if-not-resolved="false"></xref> bulk-load
(<code>source.Compile().Into(BulkSink.Insert&lt;T, TContext&gt;(factory))</code>),
writing one batch per <code>BulkInsertAsync</code> inside a single transaction so
the load is O(batch) in memory and all-or-nothing on failure.

 [EFCoreBulkSink<T, TContext\>](Flowthru.Extensions.EFCore.Bulk.EFCoreBulkSink\-2.md)

A streaming <xref href="Flowthru.Prelude.IFlowSink%601" data-throw-if-not-resolved="false"></xref> that bulk-inserts a
<xref href="Flowthru.Prelude.FlowSource%601" data-throw-if-not-resolved="false"></xref> into an EF Core table <em>one batch at a time,
inside a single transaction</em>. Driven by
<xref href="Flowthru.Prelude.FlowSourceCompiler%601.Into(Flowthru.Prelude.IFlowSink%7b%600%7d)" data-throw-if-not-resolved="false"></xref>: a context + transaction open once
(<xref href="Flowthru.Extensions.EFCore.Bulk.EFCoreBulkSink%602.OpenAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>), each arriving batch issues its own
<code>BulkInsertAsync</code> enlisted in that transaction
(<xref href="Flowthru.Extensions.EFCore.Bulk.EFCoreBulkSink%602.WriteBatchAsync(System.Collections.Generic.IReadOnlyList%7b%600%7d%2cSystem.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>), and the transaction commits on success
(<xref href="Flowthru.Extensions.EFCore.Bulk.EFCoreBulkSink%602.CompleteAsync(System.Threading.CancellationToken)" data-throw-if-not-resolved="false"></xref>).

