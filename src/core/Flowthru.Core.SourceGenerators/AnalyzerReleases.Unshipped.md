; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category         | Severity | Notes
--------|------------------|----------|----------------------------------------
FT0001  | Flowthru.Algebra | Warning  | Switch over closed sum is missing case
FT1001  | Flowthru.Schema  | Error    | FlowthruSchema type must be partial
FT1002  | Flowthru.Schema  | Warning  | Conflicting manual schema interface
