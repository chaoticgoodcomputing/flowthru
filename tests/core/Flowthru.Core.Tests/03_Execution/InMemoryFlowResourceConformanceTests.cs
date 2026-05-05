using Flowthru.Tests.Kits.Effects;

namespace Flowthru.Core.Tests.Execution;

/// <summary>
/// Self-test: runs the kit's <see cref="EphemeralResourceConformance{TBackend, TScope}"/>
/// against an in-memory backend. Any failure here is a kit bug, not a
/// provider bug — the in-memory backend is trivially correct.
/// </summary>
[TestFixture]
[Category("Execution")]
public class InMemoryFlowResourceConformanceTests
  : EphemeralResourceConformance<InMemoryEphemeralResourceBackend, int>;
