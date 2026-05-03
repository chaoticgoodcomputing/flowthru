; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category              | Severity | Notes                                                                       |
| ------- | --------------------- | -------- | --------------------------------------------------------------------------- |
| FT1001  | Flowthru.Schema       | Error    | FlowthruSchema type must be partial                                         |
| FT1002  | Flowthru.Schema       | Warning  | Conflicting manual schema interface                                         |
| FT1003  | Flowthru.Schema       | Error    | FlowthruColumn backing type must be a recognized scalar                     |
| FT1004  | Flowthru.Schema       | Error    | FlowthruColumn declarations disagree on backing type                        |
| FT2001  | Flowthru.Registration | Error    | Pipeline requires catalog not registered via RegisterCatalog                |
| FT2002  | Flowthru.Registration | Warning  | Catalog registered but not referenced by any pipeline                       |
| FT2003  | Flowthru.Registration | Warning  | Concrete pipeline parameter resolved from DI; consider configurationSection |
| FT2004  | Flowthru.Registration | Error    | configurationSection specified but UseConfiguration() not called            |
| FT4001  | Flowthru.Core.Steps   | Warning  | Step factory class missing [FlowthruStep] attribute                         |
| FT4002  | Flowthru.Core.Steps   | Warning  | Step service has no registered IFlowthruInspector                           |
| FT4003  | Flowthru.Core.Steps   | Hidden   | Step with service dependencies lacks declared traits                        |
