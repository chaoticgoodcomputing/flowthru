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

