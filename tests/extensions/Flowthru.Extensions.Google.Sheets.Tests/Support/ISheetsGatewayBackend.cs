using Flowthru.Data.Storage.Sheets;
using Flowthru.Tests.Kits.Prelude;

namespace Flowthru.Extensions.Google.Sheets.Tests.Support;

/// <summary>
/// Backend abstraction for <see cref="Contract.SheetsGatewayLaws{TBackend}"/> —
/// the Sheets analogue of the EF Core backend matrix, but for a <em>behavioral
/// contract over a live object</em> rather than a resource bracket.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why not <c>IResourceBackend</c>?</strong> The canonical backend
/// matrix kit (<c>FlowResourceLaws</c> / <c>IResourceBackend</c>) verifies an
/// acquire/release bracket: <c>CreateResource()</c> returns a
/// <c>FlowResource&lt;TScope&gt;</c> and the inherited laws assert "acquire
/// creates external state, release drops it". The Sheets gateway has no such
/// bracket — <see cref="ISheetsGateway"/> is a plain seam whose laws are
/// round-trip behaviours (resolve / add / replace / read), and Flowthru
/// deliberately <em>never</em> drops a spreadsheet on release (it creates
/// tables, not spreadsheets). Subclassing <c>FlowResourceLaws</c> would
/// inherit four bracket laws that are meaningless — or actively wrong — for
/// this seam. So this kit reuses the parts of the convention that fit (the
/// <c>[TestFixture(typeof(TBackend))]</c> matrix shape, plus
/// <see cref="TestCapability"/> / <see cref="TestCapabilities"/> gating via
/// <c>Assume.That</c> in <c>OneTimeSetUp</c>) and defines its own gateway-shaped
/// backend interface instead of forcing the resource-lifecycle one.
/// </para>
/// <para>
/// <strong>Disjoint-state contract.</strong> A single backend instance lives
/// for a whole fixture; <see cref="CreateResource"/> is called per test and must
/// return a gateway + addressing context whose external state is disjoint from
/// every prior call (a fresh temp file for offline; a unique table-name prefix
/// inside a shared spreadsheet for live), so tests never observe each other's
/// effects.
/// </para>
/// <para>
/// <strong>Constructor contract.</strong> Like <see cref="IResourceBackend{T}"/>,
/// constructors must be cheap and configuration-only — no network, no browser
/// consent, no <c>SheetsService</c> build. Expensive shared setup belongs in
/// <see cref="InitializeAsync"/>, which the laws kit runs only after the
/// <see cref="RequiredCapabilities"/> gate clears.
/// </para>
/// </remarks>
public interface ISheetsGatewayBackend
{
  /// <summary>
  /// Capabilities this backend depends on. The laws kit's <c>OneTimeSetUp</c>
  /// runs <see cref="TestCapability.IsAvailable"/> over this list via
  /// <c>Assume.That</c> before any <see cref="InitializeAsync"/> or
  /// <see cref="CreateResource"/> call — a missing capability yields an
  /// Inconclusive fixture rather than a failure. Empty for backends with no
  /// external dependency (the offline tier).
  /// </summary>
  IReadOnlyList<TestCapability> RequiredCapabilities => [];

  /// <summary>
  /// Expensive shared setup that needs an async context (building an
  /// authenticated <c>SheetsService</c>, resolving the test spreadsheet).
  /// Invoked once per fixture by the laws kit <em>after</em> the capability
  /// gate clears, so a missing credential never triggers a browser consent or
  /// a network call. No-op by default.
  /// </summary>
  Task InitializeAsync() => Task.CompletedTask;

  /// <summary>
  /// Build a fresh gateway + addressing context for one test. The returned
  /// context's external state must be disjoint from every prior call (see the
  /// type-level disjoint-state contract).
  /// </summary>
  SheetsGatewayContext CreateResource();

  /// <summary>
  /// Tear down every piece of external state the fixture created — temp files
  /// for offline; the live tables this fixture's resources created (and
  /// <em>only</em> those, never sibling tables or tabs) for live. Best-effort;
  /// invoked once from <c>OneTimeTearDown</c>.
  /// </summary>
  Task Cleanup() => Task.CompletedTask;
}

/// <summary>
/// The per-test addressing context a backend hands the laws: the gateway under
/// test, the spreadsheet id to address, and a backend-unique table-name prefix
/// that keeps each test's tables disjoint from every other test's (and, on the
/// live tier, from any sibling tables already in the shared test spreadsheet).
/// </summary>
/// <param name="Gateway">The <see cref="ISheetsGateway"/> under test.</param>
/// <param name="SpreadsheetId">
/// The spreadsheet the laws address. Offline: a freshly-registered id on a
/// per-test temp store. Live: the env-configured pre-existing test spreadsheet,
/// shared across the fixture.
/// </param>
/// <param name="TableNamePrefix">
/// A unique-per-resource prefix the laws prepend to every table name they
/// create. This is what makes live state disjoint (each resource writes tables
/// no other resource or sibling touches) and what the live backend keys its
/// cleanup off.
/// </param>
public sealed record SheetsGatewayContext(
  ISheetsGateway Gateway,
  string SpreadsheetId,
  string TableNamePrefix)
{
  /// <summary>Qualify a logical table name with this context's unique prefix.</summary>
  public string Table(string logicalName) => $"{TableNamePrefix}{logicalName}";
}
