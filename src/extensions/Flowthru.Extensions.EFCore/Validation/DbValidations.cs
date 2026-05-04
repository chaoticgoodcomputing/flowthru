using Flowthru.Core.Effects;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Validation;

/// <summary>
/// EF Core-shaped <see cref="FlowValidation"/> helpers. Used from
/// <c>CatalogAbstract.Validate</c> overrides to surface connection or
/// permission problems as accumulated pre-flight failures rather than runtime
/// crashes.
/// </summary>
public static class DbValidations
{
  /// <summary>
  /// Validates that a fresh context produced by <paramref name="contextFactory"/>
  /// can establish a connection to its underlying database <strong>in its
  /// current state</strong>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use this for <strong>persistent</strong> catalogs whose database is
  /// expected to already exist when pre-flight runs. For
  /// <strong>ephemeral</strong> catalogs whose database is created by
  /// <c>EFCoreResources.EphemeralDatabase</c> during resource acquisition,
  /// use <see cref="IsConfigured{TContext}"/> instead — pre-flight runs
  /// <em>before</em> acquire, so the database doesn't yet exist and SQLite's
  /// <c>CanConnect</c> will return <c>false</c>.
  /// </para>
  /// <para>
  /// Provider semantics:
  /// </para>
  /// <list type="bullet">
  /// <item>
  ///   <description>
  ///     <strong>SQLite</strong> — passes only if the file already exists.
  ///     SQLite's <c>CanConnect</c> probes file presence; a missing file
  ///     fails the check. Inappropriate for ephemeral databases.
  ///   </description>
  /// </item>
  /// <item>
  ///   <description>
  ///     <strong>PostgreSQL / SQL Server / others</strong> — opens a real
  ///     network connection. Failure surfaces credential, hostname, or
  ///     network reachability problems early.
  ///   </description>
  /// </item>
  /// </list>
  /// <para>
  /// Synchronous — runs inline within the catalog's <c>Validate</c> override
  /// during pre-flight. For most local providers this is fast; remote
  /// connections may briefly block while the handshake completes.
  /// </para>
  /// </remarks>
  public static FlowValidation CanConnect<TContext>(IDbContextFactory<TContext> contextFactory)
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);

    var source = typeof(TContext).Name;
    try
    {
      using var ctx = contextFactory.CreateDbContext();
      if (ctx.Database.CanConnect())
      {
        return FlowValidation.Pass;
      }

      return FlowValidation.Fail(
        source: source,
        message: $"Cannot establish connection for {source}."
      );
    }
    catch (Exception ex)
    {
      return FlowValidation.Fail(
        source: source,
        message: $"Connection check for {source} threw: {ex.Message}",
        exception: ex
      );
    }
  }

  /// <summary>
  /// Validates that <paramref name="contextFactory"/> is well-configured —
  /// it can produce a context with a non-empty connection string without
  /// throwing. Does <strong>not</strong> attempt a real connection.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Appropriate for <strong>ephemeral</strong> catalogs whose database is
  /// created by a <c>FlowResource</c> during acquisition: the database
  /// doesn't exist at pre-flight time, so a real connection check would
  /// produce a self-defeating false negative. <c>IsConfigured</c> catches
  /// genuine misconfiguration (missing options, malformed connection string,
  /// DI mistakes) without requiring the database to exist.
  /// </para>
  /// <para>
  /// For persistent catalogs whose database must already exist, prefer
  /// <see cref="CanConnect{TContext}"/>.
  /// </para>
  /// </remarks>
  public static FlowValidation IsConfigured<TContext>(IDbContextFactory<TContext> contextFactory)
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);

    var source = typeof(TContext).Name;
    try
    {
      using var ctx = contextFactory.CreateDbContext();
      var connectionString = ctx.Database.GetConnectionString();
      if (string.IsNullOrWhiteSpace(connectionString))
      {
        return FlowValidation.Fail(
          source: source,
          message: $"{source} factory produced a context with an empty connection string."
        );
      }
      return FlowValidation.Pass;
    }
    catch (Exception ex)
    {
      return FlowValidation.Fail(
        source: source,
        message: $"Configuration check for {source} threw: {ex.Message}",
        exception: ex
      );
    }
  }
}
