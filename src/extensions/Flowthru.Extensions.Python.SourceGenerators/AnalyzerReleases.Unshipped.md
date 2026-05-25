; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID  | Category            | Severity | Notes                                                                |
| -------- | ------------------- | -------- | -------------------------------------------------------------------- |
| FTPY1501 | Flowthru.Validation | Error    | Python package declared by a capability is missing from uv.lock      |
| FTPY1502 | Flowthru.Validation | Error    | Locked Python package version fails declared capability constraint  |
| FTPY2007 | Flowthru.Validation | Warning  | Python decorator references unknown schema                           |
| FTPY2008 | Flowthru.Validation | Error    | Python step schema contains a property type Arrow cannot marshal     |
| FTPY2009 | Flowthru.Validation | Error    | Python step type argument contains a property type Arrow cannot marshal |
| FTPY3007 | Flowthru.Validation | Error    | Pre-flight (Python): worker missing                                  |
| FTPY3008 | Flowthru.Validation | Error    | Pre-flight (Python): module not on search path                       |
| FTPY3009 | Flowthru.Validation | Error    | Pre-flight (Python): decorator-less module                           |
| FTPY3010 | Flowthru.Validation | Error    | Pre-flight (Python): decorator schema not registered                 |
| FTPY3011 | Flowthru.Validation | Error    | Pre-flight (Python): declared package missing from venv              |
| FTPY3012 | Flowthru.Validation | Error    | Pre-flight (Python): installed package version fails declared constraint |
| FTPY4007 | Flowthru.Runtime    | Error    | Runtime (Python): worker startup failed                              |
| FTPY4008 | Flowthru.Runtime    | Error    | Runtime (Python): worker crashed                                     |
| FTPY4009 | Flowthru.Runtime    | Error    | Runtime (Python): worker timed out                                   |
| FTPY4010 | Flowthru.Runtime    | Error    | Runtime (Python): step body raised                                   |
| FTPY4011 | Flowthru.Runtime    | Error    | Runtime (Python): worker protocol error                              |
| FTPY4012 | Flowthru.Runtime    | Error    | Runtime (Python): unsupported decorator shape                        |
