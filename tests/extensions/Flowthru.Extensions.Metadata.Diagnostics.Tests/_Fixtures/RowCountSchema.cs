using Flowthru.Data.Schema;

namespace Flowthru.Extensions.Metadata.Diagnostics.Tests.Fixtures;

/// <summary>
/// Trivial schema used by row-count tests that need a directory of
/// JSON documents. Lives at the top level (not nested inside a test
/// class) so the <c>[FlowthruSchema]</c> source generator emits the
/// <see cref="IStructuredSerializable"/> marker the
/// <c>DirectoryItemFactory.JsonDocuments&lt;T&gt;</c> constraint
/// requires.
/// </summary>
[FlowthruSchema]
public partial record RowCountSchema
{
  public required string Id { get; init; }
}
