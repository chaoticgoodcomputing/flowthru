# <a id="Flowthru_Core_Results"></a> Namespace Flowthru.Core.Results

### Classes

 [ConsoleResultFormatter](Flowthru.Core.Results.ConsoleResultFormatter.md)

Formats pipeline results as human-readable console output.

 [FlowExecutionEscapedException](Flowthru.Core.Results.FlowExecutionEscapedException.md)

Marker exception that wraps any failure which escapes the normal
<xref href="Flowthru.Core.Flows.FlowResult" data-throw-if-not-resolved="false"></xref> contract — i.e., a runtime failure that
surfaced as a thrown exception rather than a structured step failure.

 [GitHubIssueUrlBuilder](Flowthru.Core.Results.GitHubIssueUrlBuilder.md)

Builds a pre-filled GitHub issue URL from a <xref href="Flowthru.Core.Results.RuntimeErrorReport" data-throw-if-not-resolved="false"></xref>.

 [RuntimeErrorClassifier](Flowthru.Core.Results.RuntimeErrorClassifier.md)

Classifies runtime exceptions as external/environmental or possible framework bugs
using heuristic type matching.

 [RuntimeErrorReport](Flowthru.Core.Results.RuntimeErrorReport.md)

Captures the context of a runtime pipeline failure for error reporting.

### Interfaces

 [IFlowResultFormatter](Flowthru.Core.Results.IFlowResultFormatter.md)

Interface for formatting Flow execution results.

### Enums

 [ErrorClassification](Flowthru.Core.Results.ErrorClassification.md)

Classifies a runtime failure as either an external/environmental error
or a possible framework bug.

