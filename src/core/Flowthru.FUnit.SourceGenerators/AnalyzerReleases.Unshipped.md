; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category       | Severity | Notes                                                        |
| ------- | -------------- | -------- | ------------------------------------------------------------ |
| FU001   | Flowthru.FUnit | Warning  | FlowthruStep class has no [StepTest] methods in this project |
| FU002   | Flowthru.FUnit | Warning  | FunitContext subclass not guarded by #if FUNIT_ENABLED       |
| FU100   | Flowthru.FUnit | Warning  | Step service has no registered stub for FUnit test           |
