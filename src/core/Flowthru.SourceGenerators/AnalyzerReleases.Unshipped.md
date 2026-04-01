; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category              | Severity | Notes                                                                       |
| ------- | --------------------- | -------- | --------------------------------------------------------------------------- |
| FT1001  | Flowthru.Schema       | Error    | FlowthruSchema type must be partial                                         |
| FT1002  | Flowthru.Schema       | Warning  | Conflicting manual schema interface                                         |
| FT2001  | Flowthru.Registration | Error    | Pipeline requires catalog not registered via UseCatalog                     |
| FT2002  | Flowthru.Registration | Warning  | Catalog registered but not referenced by any pipeline                       |
| FT2003  | Flowthru.Registration | Warning  | Concrete pipeline parameter resolved from DI; consider configurationSection |
| FT2004  | Flowthru.Registration | Error    | configurationSection specified but UseConfiguration() not called            |
