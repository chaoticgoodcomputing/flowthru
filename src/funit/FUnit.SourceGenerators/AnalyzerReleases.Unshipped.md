; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
FU001   | Flowthru.FUnit | Warning | [FlowthruStep] class has no [FUnitStepTest] coverage
FU002   | Flowthru.FUnit | Warning | FUnitContext subclass not guarded by #if FUNIT_ENABLED
FU100   | Flowthru.FUnit | Warning | [FUnitStepTest] step has unregistered service dependency
