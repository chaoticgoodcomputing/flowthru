; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID     | Category                 | Severity | Notes                                                                        |
| ----------- | ------------------------ | -------- | ---------------------------------------------------------------------------- |
| FDFRAME1001 | Flowthru.Misc.DataFrames | Error    | TypedFrame Select projection body must be an object initializer              |
| FDFRAME1002 | Flowthru.Misc.DataFrames | Error    | TypedFrame Select initializer must use property-assignment bindings          |
| FDFRAME1003 | Flowthru.Misc.DataFrames | Error    | TypedFrame Select positional constructor requires a record or anonymous type |
| FDFRAME1004 | Flowthru.Misc.DataFrames | Error    | TypedFrame Aggregate result selector must be an object initializer           |
| FDFRAME1005 | Flowthru.Misc.DataFrames | Error    | TypedFrame Aggregate binding must be ctx.Key or an aggregation method call   |
