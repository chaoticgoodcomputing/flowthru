; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

| Rule ID | Category            | Severity | Notes                                                          |
| ------- | ------------------- | -------- | -------------------------------------------------------------- |
| FT0001  | Flowthru.Algebra    | Warning  | Switch over closed sum is missing case                         |
| FT1001  | Flowthru.Schema     | Error    | FlowthruSchema type must be partial                            |
| FT1002  | Flowthru.Schema     | Warning  | Conflicting manual schema interface                            |
| FT1003  | Flowthru.Schema     | Error    | FlowthruColumn property has invalid backing type               |
| FT1004  | Flowthru.Schema     | Error    | FlowthruColumn properties have inconsistent backing types      |
| FT1101  | Flowthru.Step       | Warning  | Step factory class missing [FlowthruStep] attribute            |
| FT2001  | Flowthru.Validation | Error    | Single-producer invariant violated                             |
| FT2002  | Flowthru.Validation | Error    | Step type alignment violated                                   |
| FT3001  | Flowthru.Validation | Error    | Pre-flight: duplicate producer                                 |
| FT3002  | Flowthru.Validation | Error    | Pre-flight: circular dependency                                |
| FT3003  | Flowthru.Validation | Error    | Pre-flight: missing input                                      |
| FT3004  | Flowthru.Validation | Error    | Pre-flight: schema drift                                       |
| FT3005  | Flowthru.Validation | Error    | Pre-flight: inspection failed                                  |
| FT3006  | Flowthru.Validation | Error    | Pre-flight: registration check failed                          |
| FT4001  | Flowthru.Runtime    | Error    | Runtime: external failure                                      |
| FT4002  | Flowthru.Runtime    | Error    | Runtime: step failed                                           |
| FT4003  | Flowthru.Runtime    | Error    | Runtime: cancelled                                             |
| FT4004  | Flowthru.Runtime    | Error    | Runtime: invariant violated                                    |
| FT4005  | Flowthru.Runtime    | Error    | Runtime: schema mismatch                                       |
| FT4006  | Flowthru.Runtime    | Error    | Runtime: constraint violated                                   |
| FT5001  | Flowthru.FUnit      | Warning  | FUnit context registered no fixtures                           |
