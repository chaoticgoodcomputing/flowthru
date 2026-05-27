using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SpikeAnalyzer;

// SPIKE: emits a diagnostic carrying a populated Flowthru.Anchor.*
// property bag, on any source file containing the trigger comment
// `// FLOWTHRU_SPIKE_TRIGGER`. The whole purpose is to test whether
// VSCode's diagnostic API surfaces these properties to extensions per
// ADR-0011's renderer-over-LSP model. See SPIKE.md.

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertyBagSpikeAnalyzer : DiagnosticAnalyzer
{
    private const string TriggerMarker = "FLOWTHRU_SPIKE_TRIGGER";

    private static readonly DiagnosticDescriptor Rule = new(
        id: "FLSPIKE001",
        title: "Spike: property-bag carriage test",
        messageFormat: "SPIKE diagnostic for property-bag validation",
        category: "Spike",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Throwaway diagnostic. If you see this in production code, "
            + "the property-bag spike was left behind by accident."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var trivia in root.DescendantTrivia())
        {
            if (!trivia.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineCommentTrivia))
                continue;
            if (trivia.ToString().IndexOf(TriggerMarker, System.StringComparison.Ordinal) < 0)
                continue;

            var properties = ImmutableDictionary.CreateBuilder<string, string?>();
            // Composite numbered + flat schema per ADR-0011. The bag is
            // shaped to exercise three carriage-failure-modes in one
            // diagnostic: multi-slot numbering within a kind (Step.0 +
            // Step.1), mixed kinds (Step + Item + Edge), and a different
            // kind prefix structure (Edge.From + Edge.To, not Edge.0.Label).
            properties.Add("Flowthru.Anchor.Step.0.Label", "spike_step");
            properties.Add("Flowthru.Anchor.Step.0.Flow", "spike_flow");
            properties.Add("Flowthru.Anchor.Step.1.Label", "spike_step_two");
            properties.Add("Flowthru.Anchor.Step.1.Flow", "spike_flow");
            properties.Add("Flowthru.Anchor.Item.0.Label", "spike_item");
            properties.Add("Flowthru.Anchor.Edge.0.From", "spike_step");
            properties.Add("Flowthru.Anchor.Edge.0.To", "spike_item");

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                trivia.GetLocation(),
                properties.ToImmutable()
            ));
        }
    }
}
