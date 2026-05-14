# <a id="Flowthru_FUnit_CodeFixes"></a> Namespace Flowthru.FUnit.CodeFixes

### Classes

 [Fu001ScaffoldTestsClassFix](Flowthru.FUnit.CodeFixes.Fu001ScaffoldTestsClassFix.md)

Code fix for FU001: scaffolds a stub <code>Tests : FUnitContext</code> class inside a
<code>#if FUNIT_ENABLED</code> / <code>#endif</code> block at the end of the step class body,
and inserts a <code>using Flowthru.Step.Testing;</code> directive if missing. The
scaffold contains a placeholder <code>[FUnitStepTest]</code>-decorated method so
applying the fix actually clears the FU001 diagnostic — the analyzer requires at
least one <code>[FUnitStepTest]</code> method, not just an empty <code>Tests</code> class.

 [Fu002WrapWithFUnitEnabledFix](Flowthru.FUnit.CodeFixes.Fu002WrapWithFUnitEnabledFix.md)

Code fix for FU002: wraps a <code>FUnitContext</code> subclass with
<code>#if FUNIT_ENABLED</code> / <code>#endif</code> preprocessor guards.

 [Fu100AddStubRegistrationFix](Flowthru.FUnit.CodeFixes.Fu100AddStubRegistrationFix.md)

Code fix for FU100: when a <code>[FUnitStepTest]</code> references a step whose
service dependency has no registered stub, this fix inserts a registration
template into an existing <code>[FUnitStubContainer]</code> in the project. If
no container exists, it scaffolds a new one in the test class's namespace.

