# <a id="Flowthru_Core_Data_ParquetItemOptions_1"></a> Class ParquetItemOptions<TRow\>

Namespace: [Flowthru.Core.Data](Flowthru.Core.Data.md)  
Assembly: Flowthru.Extensions.Parquet.dll  

Performance and behavior tuning options for Parquet catalog entries.

```csharp
public sealed record ParquetItemOptions<TRow> : IEquatable<ParquetItemOptions<TRow>> where TRow : notnull, IFlatSchema, IBinarySerializable
```

#### Type Parameters

`TRow` 

The row schema type this options object is bound to.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ParquetItemOptions<TRow\>](Flowthru.Core.Data.ParquetItemOptions\-1.md)

#### Implements

[IEquatable<ParquetItemOptions<TRow\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

#### Inherited Members

[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring)

## Remarks

<p>
Pass an instance to <xref href="Flowthru.Core.Data.ParquetItemExtensions.Parquet%60%601(Flowthru.Core.Data.EnumerableItemFactory%2cSystem.String%2cSystem.String%2cFlowthru.Core.Data.ParquetItemOptions%7b%60%600%7d%2cFlowthru.Core.Data.Storage.IStorageMediumResolver%2cFlowthru.Core.Data.Storage.IStorageMedium)" data-throw-if-not-resolved="false"></xref> to override defaults.
A bare catalog entry with no options uses production-ready defaults automatically:
</p>
<ul><li><b>RowGroupSize</b> — 1 000 000 rows (Parquet.Net default). The write path streams rows
in batches of this size, keeping peak write-side memory bounded regardless of dataset size.</li><li><b>CompressionMethod</b> — Snappy (best latency/ratio balance for analytic workloads).</li><li><b>UseDictionaryEncoding</b> — true (automatic dictionary encoding for low-cardinality
columns such as categorical strings and IDs).</li></ul>
<p>
<b>Row group sizing guidance (per Parquet best practices):</b>
Target 128 MB – 512 MB of uncompressed row data per group. At ~100 bytes/row, 1 000 000 rows
≈ 100 MB — a reasonable default. For wider rows, reduce <xref href="Flowthru.Core.Data.ParquetItemOptions%601.RowGroupSize" data-throw-if-not-resolved="false"></xref>. For
narrower rows (e.g. pure numeric), you can increase it.
</p>
<p>
<b>Compression guidance:</b>
<ul><li><xref href="Parquet.CompressionMethod.Snappy" data-throw-if-not-resolved="false"></xref> — low CPU, fast decompression; best for interactive
and real-time workloads.</li><li><xref href="Parquet.CompressionMethod.Zstd" data-throw-if-not-resolved="false"></xref> — tunable; better ratio than Snappy at moderate CPU
cost; suitable for cold/archival paths.</li><li><xref href="Parquet.CompressionMethod.Gzip" data-throw-if-not-resolved="false"></xref> — highest ratio, slowest; use when storage cost
dominates query latency requirements.</li></ul>
</p>
<p>
<b>Per-column encoding hints</b> (e.g. Delta encoding for sorted ID columns) require
<code>ColumnEncodingHints</code>, which is a Parquet.Net v6+ API not yet published to NuGet.
This will be surfaced via a <code>WithEncodingHint(expr, hint)</code> fluent method once v6 is stable.
In the meantime, <xref href="Flowthru.Core.Data.ParquetItemOptions%601.UseDictionaryEncoding" data-throw-if-not-resolved="false"></xref> and <xref href="Flowthru.Core.Data.ParquetItemOptions%601.UseDeltaBinaryPackedEncoding" data-throw-if-not-resolved="false"></xref>
apply globally.
</p>

## Properties

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_CompressionLevel"></a> CompressionLevel

Compression level hint passed to the chosen codec. Defaults to <xref href="System.IO.Compression.CompressionLevel.Optimal" data-throw-if-not-resolved="false"></xref>.

```csharp
public CompressionLevel CompressionLevel { get; init; }
```

#### Property Value

 [CompressionLevel](https://learn.microsoft.com/dotnet/api/system.io.compression.compressionlevel)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_CompressionMethod"></a> CompressionMethod

Compression algorithm applied to each data page. Defaults to <xref href="Parquet.CompressionMethod.Snappy" data-throw-if-not-resolved="false"></xref>.

```csharp
public CompressionMethod CompressionMethod { get; init; }
```

#### Property Value

 [CompressionMethod](https://github.com/aloneguid/parquet\-dotnet/blob/92cca5438bcd7a5e7bffbbe1c91c63b427dc7b97/src/Parquet/CompressionMethod.cs)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_DictionaryEncodingThreshold"></a> DictionaryEncodingThreshold

Uniqueness factor threshold (0–1) below which dictionary encoding is applied.
Defaults to 0.8 (i.e. dictionary encoding when ≤ 80% of values are unique).

```csharp
public double DictionaryEncodingThreshold { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_MaximumLargePoolFreeBytes"></a> MaximumLargePoolFreeBytes

Maximum bytes kept in the large-object pool before the GC may reclaim them.
Defaults to 64 MB.

```csharp
public int MaximumLargePoolFreeBytes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_MaximumSmallPoolFreeBytes"></a> MaximumSmallPoolFreeBytes

Maximum bytes kept in the small-object pool before the GC may reclaim them.
Defaults to 16 MB. Reduce to lower peak memory; increase to reduce GC pressure on
write-heavy workloads.

```csharp
public int MaximumSmallPoolFreeBytes { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_RowGroupSize"></a> RowGroupSize

Number of rows per row group on write. Defaults to 1 000 000.

```csharp
public int RowGroupSize { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Remarks

The write path buffers up to this many rows in memory, then flushes one row group to disk.
Peak write-side memory is bounded to approximately <code>RowGroupSize × (row byte width)</code>.

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseBigDecimal"></a> UseBigDecimal

Use <code>BigDecimal</code> instead of <code>decimal</code> for high-precision decimal columns.
Defaults to <code>false</code>.

```csharp
public bool UseBigDecimal { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseDateOnlyForDates"></a> UseDateOnlyForDates

Deserialize Parquet DATE columns as <xref href="System.DateOnly" data-throw-if-not-resolved="false"></xref> instead of <xref href="System.DateTime" data-throw-if-not-resolved="false"></xref>.
Defaults to <code>false</code> for backwards compatibility.

```csharp
public bool UseDateOnlyForDates { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseDeltaBinaryPackedEncoding"></a> UseDeltaBinaryPackedEncoding

Enable delta-binary-packed encoding globally for integer columns. Defaults to <code>false</code>.

```csharp
public bool UseDeltaBinaryPackedEncoding { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Most effective on monotonically increasing integer columns (auto-increment IDs, timestamps)
where successive delta values are small. May slightly increase CPU on read. Leave at
<code>false</code> unless you have profiled this as a bottleneck.

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseDictionaryEncoding"></a> UseDictionaryEncoding

Enable dictionary encoding globally. Defaults to <code>true</code>.

```csharp
public bool UseDictionaryEncoding { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Remarks

Dictionary encoding stores repeated values (low-cardinality columns like enums, categories,
product codes) as integer references to a dictionary side-table. This typically halves storage
for such columns. Disable only if all columns have near-100% unique values.

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseTimeOnlyForTimeMicros"></a> UseTimeOnlyForTimeMicros

Deserialize Parquet TIME (microsecond precision) columns as <xref href="System.TimeOnly" data-throw-if-not-resolved="false"></xref>.
Defaults to <code>false</code>.

```csharp
public bool UseTimeOnlyForTimeMicros { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="Flowthru_Core_Data_ParquetItemOptions_1_UseTimeOnlyForTimeMillis"></a> UseTimeOnlyForTimeMillis

Deserialize Parquet TIME (millisecond precision) columns as <xref href="System.TimeOnly" data-throw-if-not-resolved="false"></xref>.
Defaults to <code>false</code>.

```csharp
public bool UseTimeOnlyForTimeMillis { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

