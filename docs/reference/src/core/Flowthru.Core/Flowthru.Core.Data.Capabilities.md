# <a id="Flowthru_Core_Data_Capabilities"></a> Namespace Flowthru.Core.Data.Capabilities

### Classes

 [FormatRowFeatures](Flowthru.Core.Data.Capabilities.FormatRowFeatures.md)

Declares which row-shape features an <xref href="Flowthru.Core.Data.Storage.IFormatSerializer%601" data-throw-if-not-resolved="false"></xref>
implementation supports. Companion to <xref href="Flowthru.Core.Data.Capabilities.StorageTraits" data-throw-if-not-resolved="false"></xref> — where
<xref href="Flowthru.Core.Data.Capabilities.StorageTraits" data-throw-if-not-resolved="false"></xref> describes <em>medium-level</em> capabilities (read/write,
streaming, transactional), <xref href="Flowthru.Core.Data.Capabilities.FormatRowFeatures" data-throw-if-not-resolved="false"></xref> describes
<em>row-shape</em> capabilities (which kinds of properties the format can round-trip).

 [StorageTraits](Flowthru.Core.Data.Capabilities.StorageTraits.md)

Describes the structural constraints and capabilities of a storage implementation.
Defaults represent filesystem-file baseline behavior.

