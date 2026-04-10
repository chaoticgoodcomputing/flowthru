# <a id="Flowthru_Core_CodeFixes"></a> Namespace Flowthru.Core.CodeFixes

### Classes

 [Ft1001AddPartialKeywordFix](Flowthru.Core.CodeFixes.Ft1001AddPartialKeywordFix.md)

Code fix for FT1001: adds the <code>partial</code> modifier to a type annotated with
<code>[FlowthruSchema]</code> that is missing the keyword.

 [Ft1002RemoveConflictingInterfaceFix](Flowthru.Core.CodeFixes.Ft1002RemoveConflictingInterfaceFix.md)

Code fix for FT1002: removes the conflicting manually-applied marker interface(s)
from a <code>[FlowthruSchema]</code> type's base list.
The generator will re-apply the correct interfaces automatically.

 [Ft2002RemoveUnusedCatalogFix](Flowthru.Core.CodeFixes.Ft2002RemoveUnusedCatalogFix.md)

Code fix for FT2002: removes the <code>RegisterCatalog</code> call that is registered
but never referenced by any flow.

