; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category            | Severity | Notes                                                                |
| ------- | ------------------- | -------- | -------------------------------------------------------------------- |
| FT2007  | Flowthru.Validation | Error    | Python decorator references unknown schema                           |
| FT3007  | Flowthru.Validation | Error    | Pre-flight (Python): worker missing                                  |
| FT3008  | Flowthru.Validation | Error    | Pre-flight (Python): module not on search path                       |
| FT3009  | Flowthru.Validation | Error    | Pre-flight (Python): decorator-less module                           |
| FT3010  | Flowthru.Validation | Error    | Pre-flight (Python): decorator schema not registered                 |
| FT4007  | Flowthru.Runtime    | Error    | Runtime (Python): worker startup failed                              |
| FT4008  | Flowthru.Runtime    | Error    | Runtime (Python): worker crashed                                     |
| FT4009  | Flowthru.Runtime    | Error    | Runtime (Python): worker timed out                                   |
| FT4010  | Flowthru.Runtime    | Error    | Runtime (Python): step body raised                                   |
| FT4011  | Flowthru.Runtime    | Error    | Runtime (Python): worker protocol error                              |
| FT4012  | Flowthru.Runtime    | Error    | Runtime (Python): unsupported decorator shape                        |
