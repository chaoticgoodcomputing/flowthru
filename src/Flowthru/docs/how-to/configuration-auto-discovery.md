# Configuration Patterns

This guide explains Flowthru's flexible configuration system that supports multiple patterns from fully code-based to fully configuration-based, following .NET's **Smart Defaults with Override** pattern.

## Recommended: Hybrid Configuration Pattern

**Best Practice** - Mix configuration and code for optimal balance:
- ✅ **Infrastructure in config**: Catalog, metadata, logging configured in `appsettings.json`
- ✅ **Pipeline registration in code**: Explicit registration for compile-time safety and discoverability
- ✅ **Pipeline parameters in config**: Easy tuning without code changes, environment-specific overrides

**Benefits:**
- Compile-time safety for pipeline wiring
- IntelliSense and refactoring support
- Easy parameter tuning per environment
- Clear separation: structure in code, values in config

**Example:**

```csharp
var app = FlowthruApplication.Create(args, builder => {
  // Load infrastructure from appsettings.json
  builder.UseConfiguration();
  
  // Register pipelines explicitly in code
  builder
    .RegisterPipelineWithConfiguration<MyCatalog, MyParams>(
      label: "DataScience",
      creator: DataSciencePipeline.Create,
      configurationSection: "Flowthru:Pipelines:DataScience"
    )
    .WithDescription("Trains ML model")
    .WithTags("ml", "training");
});
```

With `appsettings.json`:
```json
{
  "Flowthru": {
    "Catalog": {
      "Type": "MyApp.Data.MyCatalog",
      "ConstructorArgs": { "basePath": "Data" }
    },
    "Metadata": {
      "OutputDirectory": "Data/Metadata",
      "Providers": ["json", "mermaid"]
    },
    "Pipelines": {
      "DataScience": {
        "ModelParams": { "TestSize": 0.2 },
        "CrossValidationParams": { "NumFolds": 5 }
      }
    }
  }
}
```

## Alternative: Full Auto-Discovery Pattern

For simpler applications or when pipeline structure is stable, you can use full auto-discovery where everything is configured in `appsettings.json`.

**Trade-offs:**
- ⚠️ No compile-time validation of pipeline wiring
- ⚠️ Harder to discover available pipelines (must read config)
- ✅ Minimal code in Program.cs
- ✅ All configuration in one place

## Overview

**Smart Defaults with Override Pattern:**
- Configuration files provide defaults for catalog, metadata, pipelines, and logging
- Explicit fluent API calls override configuration-based defaults
- This aligns with ASP.NET Core conventions where configuration provides base settings and code provides overrides

## Configuration Schema

The full configuration schema is defined under the `Flowthru` section:

```json
{
  "Flowthru": {
    "Metadata": { ... },
    "Catalog": { ... },
    "Pipelines": { ... },
    "Logging": { ... }
  }
}
```

### Metadata Configuration

```json
"Metadata": {
  "OutputDirectory": "Data/Metadata",
  "Providers": ["json", "mermaid"],
  "Json": {
    "WriteIndented": true
  },
  "Mermaid": {
    "Direction": "LR"
  }
}
```

- **OutputDirectory**: Where metadata files are written (default: "metadata")
- **Providers**: List of metadata providers to enable (`"json"`, `"mermaid"`)
- **Json.WriteIndented**: Whether to format JSON with indentation (default: true)
- **Mermaid.Direction**: Flow direction (`"LR"`, `"TB"`, `"RL"`, `"BT"`)

### Catalog Configuration

```json
"Catalog": {
  "Type": "MyApp.Data.MyCatalog",
  "ConstructorArgs": {
    "basePath": "Data/Datasets",
    "someOtherParam": "value"
  },
  "BasePath": "alternative/path",
  "ConnectionString": "Server=localhost;..."
}
```

- **Type** *(required)*: Fully-qualified catalog class name
- **ConstructorArgs**: Dictionary of constructor parameter names to values
- **BasePath**: Convenience property, automatically passed as `basePath` constructor arg
- **ConnectionString**: Convenience property, automatically passed as `connectionString` constructor arg

**Type Resolution:**
The system searches all loaded assemblies for the specified type. You can use either:
- Namespace-qualified: `"MyApp.Data.MyCatalog"`
- Assembly-qualified: `"MyApp.Data.MyCatalog, MyApp"`

### Pipeline Configuration

```json
"Pipelines": {
  "PipelineName": {
    "Type": "MyApp.Pipelines.MyPipeline",
    "FactoryMethod": "Create",
    "Parameters": {
      "Param1": "value",
      "Param2": 42
    },
    "Description": "Pipeline description",
    "Tags": ["tag1", "tag2"]
  }
}
```

Each pipeline requires:
- **Type** *(required)*: Fully-qualified pipeline class name
- **FactoryMethod** *(required)*: Static factory method name (usually `"Create"`)
- **Parameters**: Configuration object matching the parameter type
- **Description**: Human-readable pipeline description
- **Tags**: Array of tags for categorization

**Factory Method Signature:**
```csharp
public static Pipeline Create(TCatalog catalog)
// or
public static Pipeline Create(TCatalog catalog, TParams parameters)
```

### Logging Configuration

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Flowthru": "Debug",
    "System": "Warning",
    "Microsoft": "Warning"
  }
}
```

Standard Microsoft.Extensions.Logging configuration. Refer to [.NET logging documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/).

## Usage Patterns

### Pattern 1: Hybrid (RECOMMENDED)

**Best for most applications** - Clear separation between structure (code) and values (config).

```csharp
var app = FlowthruApplication.Create(args, builder => {
  // Load infrastructure configuration
  builder.UseConfiguration();
  
  // Register pipelines explicitly with parameters from config
  builder
    .RegisterPipeline<MyCatalog>(
      label: "DataProcessing",
      creator: DataProcessingPipeline.Create
    )
    .WithDescription("Preprocesses raw data");
  
  builder
    .RegisterPipelineWithConfiguration<MyCatalog, DataScienceParams>(
      label: "DataScience",
      creator: DataSciencePipeline.Create,
      configurationSection: "Flowthru:Pipelines:DataScience"
    )
    .WithDescription("Trains ML model")
    .WithTags("ml", "training");
});
```

With `appsettings.json`:
```json
{
  "Flowthru": {
    "Catalog": {
      "Type": "MyApp.Data.MyCatalog",
      "ConstructorArgs": { "basePath": "Data" }
    },
    "Metadata": {
      "OutputDirectory": "Data/Metadata",
      "Providers": ["json", "mermaid"]
    },
    "Pipelines": {
      "DataScience": {
        "ModelParams": { "TestSize": 0.2 },
        "CrossValidationParams": { "NumFolds": 5 }
      }
    }
  }
}
```

### Pattern 2: Full Auto-Discovery (Minimal Code)

**Best for simple applications** - Everything configured, minimal code.

```csharp
var app = FlowthruApplication.Create(args, builder => {
  builder.UseConfiguration();
  // That's it! Catalog, metadata, and pipelines auto-discovered
});
```

With `appsettings.json`:
```json
{
  "Flowthru": {
    "Catalog": {
      "Type": "MyApp.Data.MyCatalog",
      "ConstructorArgs": { "basePath": "Data" }
    },
    "Metadata": {
      "Providers": ["json", "mermaid"]
    },
    "Pipelines": {
      "DataScience": {
        "Type": "MyApp.Pipelines.DataSciencePipeline",
        "FactoryMethod": "Create",
        "Parameters": {
          "ModelParams": { "TestSize": 0.2 },
          "CrossValidationParams": { "NumFolds": 5 }
        }
      }
    }
  }
}
```

### Pattern 3: Selective Overrides

**When you need custom logic** - Load most config, override specific parts.

```csharp
var app = FlowthruApplication.Create(args, builder => {
  builder.UseConfiguration();
  
  // Custom catalog overrides configuration
  builder.UseCatalog(new MyCustomCatalog("CustomPath"));
  
  // Pipelines still auto-discovered from configuration
});
```

### Pattern 4: Traditional (No Configuration)
### Pattern 4: Traditional (No Configuration)

**Maximum control** - Everything defined in code.

```csharp
var app = FlowthruApplication.Create(args, builder => {
  // Don't call UseConfiguration()
  
  builder.UseCatalog(new MyCatalog("Data"));
  
  builder.IncludeMetadata(meta => 
    meta.AddJson().AddMermaid()
  );
  
  builder.RegisterPipeline<MyCatalog, MyParams>(
    label: "DataScience",
    creator: DataSciencePipeline.Create,
    parameters: new MyParams { TestSize = 0.2 }
  );
});
```

## Override Precedence Rules

When you call `UseConfiguration()`, the following precedence applies:

1. **Explicit fluent API calls override configuration**
   - `UseCatalog()` → catalog from config ignored
   - `IncludeMetadata()` → metadata from config ignored
   - `RegisterPipeline()` / `RegisterPipelines<T>()` → pipelines from config ignored

2. **Configuration provides defaults when nothing is set explicitly**

3. **Last explicit call wins**
   ```csharp
   builder.UseCatalog(catalog1);  // This is overwritten
   builder.UseCatalog(catalog2);  // This is used
   ```

## Environment-Specific Configuration

Configuration files are layered by environment:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment overrides)
3. `appsettings.Local.json` (local developer overrides, never committed)

Environment is determined by (in order):
1. `FLOWTHRU_ENV` environment variable
2. `DOTNET_ENVIRONMENT` environment variable
3. `ASPNETCORE_ENVIRONMENT` environment variable
4. `"Production"` (default)

**Example:**
```json
// appsettings.json
{
  "Flowthru": {
    "Pipelines": {
      "DataScience": {
        "Parameters": {
          "NumFolds": 5,
          "TestSize": 0.2
        }
      }
    }
  }
}

// appsettings.Development.json
{
  "Flowthru": {
    "Pipelines": {
      "DataScience": {
        "Parameters": {
          "NumFolds": 3  // Fewer folds for faster dev iterations
        }
      }
    }
  }
}
```

In Development, `NumFolds=3` and `TestSize=0.2` (merged from base).

## Parameter Validation

All parameter objects loaded from configuration are validated using `System.ComponentModel.DataAnnotations`:

```csharp
public record ModelParams {
  [Range(0.0, 1.0)]
  public double TestSize { get; init; } = 0.2;
  
  public int RandomState { get; init; } = 42;
}
```

Invalid configuration throws `ValidationException` with detailed messages.

## Type Discovery

The system automatically discovers types from all loaded assemblies:

- **Catalog types**: Must inherit from `DataCatalogBase`
- **Pipeline types**: Must contain the specified factory method
- **Parameter types**: Must have parameterless constructor and settable properties

**Best Practices:**
- Use namespace-qualified names: `"MyApp.Pipelines.DataSciencePipeline"`
- Assembly-qualified names work but are unnecessary: `"MyApp.Pipelines.DataSciencePipeline, MyApp"`
- Ensure the assembly containing the type is loaded (reference it in your project)

## Advanced: Custom Catalog Factory

Implement `ICatalogFactory` for advanced catalog construction logic:

```csharp
public class DatabaseCatalogFactory : ICatalogFactory {
  public DataCatalogBase CreateCatalog(CatalogOptions options, IServiceProvider serviceProvider) {
    var environment = Environment.GetEnvironmentVariable("FLOWTHRU_ENV") ?? "Production";
    
    return environment switch {
      "Development" => new LocalFileCatalog(options.BasePath ?? "Data"),
      "Production" => new DatabaseCatalog(options.ConnectionString!),
      _ => throw new InvalidOperationException($"Unknown environment: {environment}")
    };
  }
}

// Register in builder
builder.Services.AddSingleton<ICatalogFactory, DatabaseCatalogFactory>();
```

Then in configuration:
```json
{
  "Flowthru": {
    "Catalog": {
      "Type": "MyApp.Factories.DatabaseCatalogFactory",
      "ConnectionString": "Server=..."
    }
  }
}
```

## See Also

- [Configuration Files](configuration-files.md) - Basic configuration setup and layering
- [Parameter Validation](parameter-validation.md) - Using DataAnnotations for validation
- [Pipeline Registration](../tutorials.md#pipeline-registration) - Traditional registration approaches
