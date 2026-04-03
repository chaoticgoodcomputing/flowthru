# <a id="Flowthru_Configuration_ICatalogFactory"></a> Interface ICatalogFactory

Namespace: [Flowthru.Configuration](Flowthru.Configuration.md)  
Assembly: Flowthru.Core.dll  

Factory interface for creating data catalog instances from configuration.

```csharp
public interface ICatalogFactory
```

## Remarks

Implement this interface to enable configuration-based catalog construction.
The factory receives the full configuration and can use it to construct
environment-specific catalogs (e.g., local files in dev, remote DB in prod).

## Methods

### <a id="Flowthru_Configuration_ICatalogFactory_CreateCatalog_Flowthru_Configuration_CatalogOptions_System_IServiceProvider_"></a> CreateCatalog\(CatalogOptions, IServiceProvider\)

Creates a catalog instance based on configuration.

```csharp
CatalogAbstract CreateCatalog(CatalogOptions options, IServiceProvider serviceProvider)
```

#### Parameters

`options` [CatalogOptions](Flowthru.Configuration.CatalogOptions.md)

Catalog configuration options

`serviceProvider` [IServiceProvider](https://learn.microsoft.com/dotnet/api/system.iserviceprovider)

Service provider for dependency injection

#### Returns

 [CatalogAbstract](Flowthru.Data.CatalogAbstract.md)

The configured catalog instance

