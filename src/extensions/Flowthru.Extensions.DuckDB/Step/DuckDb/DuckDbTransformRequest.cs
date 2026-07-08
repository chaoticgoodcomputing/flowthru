namespace Flowthru.Step.DuckDb;

/// <summary>
/// Everything <see cref="IDuckDbEngine.ExecuteTransform"/> needs to run
/// one engine-delegated transform. Built by <c>DuckDbTransformStep</c>
/// at execution time, after each endpoint's bytes have been located.
/// </summary>
/// <param name="StepLabel">
/// Label of the flow step this request executes for — used to attribute
/// error values and diagnostics.
/// </param>
/// <param name="Relations">
/// The input relations, in wire-up order: each SQL relation name bound
/// to the local Parquet file holding its rows.
/// </param>
/// <param name="Sql">
/// The transform body — a single SQL query whose relations are the
/// names in <paramref name="Relations"/> and whose result becomes the
/// output item's rows.
/// </param>
/// <param name="OutputPath">
/// Local filesystem path the result is written to as Parquet
/// (DuckDB <c>COPY ... TO</c>); parent directories are created if
/// missing.
/// </param>
/// <param name="ExpectedColumns">
/// The output item's declared schema, one entry per column, that the
/// SQL's result schema is verified against before anything is written.
/// </param>
/// <param name="Options">Output-write tuning (compression, row-group size).</param>
public sealed record DuckDbTransformRequest(
  string StepLabel,
  IReadOnlyList<DuckDbBoundRelation> Relations,
  string Sql,
  string OutputPath,
  IReadOnlyList<DuckDbExpectedColumn> ExpectedColumns,
  DuckDbTransformOptions Options
);

/// <summary>
/// One input relation, resolved to bytes: the name the transform SQL
/// refers to it by, and the local Parquet file the engine reads it from.
/// </summary>
/// <param name="Name">SQL relation name (quoted by the engine — any non-empty string works).</param>
/// <param name="LocalPath">Absolute path of the Parquet file holding the relation's rows.</param>
public sealed record DuckDbBoundRelation(string Name, string LocalPath);

/// <summary>
/// One column of the output item's declared schema, as the engine
/// verifies it: the serialized column name and the CLR type the column
/// must be readable back as.
/// </summary>
/// <param name="Name">
/// Column name in the output file — the schema property's name, or its
/// <c>[SerializedLabel]</c> override. Matched case-insensitively against
/// the SQL result's column names.
/// </param>
/// <param name="ClrType">
/// The non-nullable CLR type the column round-trips as (enum properties
/// verify as their underlying integer type, matching how Parquet stores
/// them).
/// </param>
/// <param name="IsNullable">
/// Whether the schema property admits null. Informational in v1: DuckDB
/// reports every query result column as nullable, so nullability is
/// enforced when the output is next loaded, not at transform time.
/// </param>
public sealed record DuckDbExpectedColumn(string Name, Type ClrType, bool IsNullable);

/// <summary>
/// What <see cref="IDuckDbEngine.ExecuteTransform"/> reports back after
/// a successful transform.
/// </summary>
/// <param name="RowsCopied">
/// Number of rows the engine wrote to the output file, as reported by
/// DuckDB's <c>COPY</c>. Informational — <c>0</c> when the engine
/// version doesn't report a count.
/// </param>
/// <param name="ResultColumns">
/// The result schema DuckDB produced, one <c>(name, duckDbType)</c> pair
/// per column — the schema that passed verification.
/// </param>
public sealed record DuckDbTransformResult(
  long RowsCopied,
  IReadOnlyList<(string Name, string DuckDbType)> ResultColumns
);
