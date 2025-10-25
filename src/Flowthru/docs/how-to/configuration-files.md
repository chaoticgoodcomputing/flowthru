# How to Use Configuration Files

Flowthru supports external configuration files for pipeline parameters, making it easy to:
- Switch between environments (Development, Production, etc.)
- Share base configuration across teams
- Keep sensitive or user-specific settings out of source control

This guide covers the configuration system and migration from inline parameters.

---

## Quick Start

### Step 1: Enable Configuration

Call `.UseConfiguration()` in your application builder:

```csharp
public static async Task<int> Main(string[] args) {
  return await FlowthruApplication.Create(args, builder => {
    builder.UseConfiguration();  // Enable configuration loading
    builder.UseCatalog(new MyCatalog());
    // ... register pipelines
  });
}
```

### Step 2: Create Configuration Files

Create `appsettings.json` in your project root:

```json
{
  "DataScience": {
    "ModelParams": {
      "TestSize": 0.2,
      "RandomState": 3
    },
    "CrossValidationParams": {
      "NumFolds": 5,
      "BaseSeed": 42,
      "KedroReferenceR2Score": 0.387
    }
  }
}
```

### Step 3: Register Pipeline with Configuration

Use `.RegisterPipelineWithConfiguration()`:

```csharp
builder
  .RegisterPipelineWithConfiguration<MyCatalog, DataSciencePipelineParams>(
    label: "DataScience",
    creator: DataSciencePipeline.Create,
    configurationSection: "DataScience"
  )
  .WithDescription("Trains and evaluates ML model");
```

---

## Configuration Layering

Flowthru uses Microsoft.Extensions.Configuration's layered approach. Configuration files are merged in this order (later files override earlier):

1. **`appsettings.json`** - Base configuration (required)
2. **`appsettings.{Environment}.json`** - Environment-specific overrides (optional)
3. **`appsettings.Local.json`** - Local/user-specific overrides (optional, gitignored)

### Example: Environment-Specific Configuration

**Base configuration (appsettings.json):**
```json
{
  "DataScience": {
    "CrossValidationParams": {
      "NumFolds": 5,
      "BaseSeed": 42,
      "KedroReferenceR2Score": 0.387
    }
  }
}
```

**Development override (appsettings.Development.json):**
```json
{
  "DataScience": {
    "CrossValidationParams": {
      "NumFolds": 3
    }
  }
}
```

When running with `FLOWTHRU_ENV=Development`, the final configuration merges to:
```json
{
  "DataScience": {
    "CrossValidationParams": {
      "NumFolds": 3,
      "BaseSeed": 42,
      "KedroReferenceR2Score": 0.387
    }
  }
}
```

---

## Environment Resolution

Flowthru resolves the environment name in this priority order:

1. Explicit configuration: `.WithEnvironment("Development")`
2. Custom environment variable (default: `FLOWTHRU_ENV`)
3. Standard .NET variables: `DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT`
4. Default: `"Production"`

### Examples

**Use environment variable:**
```bash
FLOWTHRU_ENV=Development dotnet run DataScience
```

**Set explicitly in code:**
```csharp
builder.UseConfiguration(config => config
  .WithEnvironment("Development"));
```

**Use custom environment variable:**
```csharp
builder.UseConfiguration(config => config
  .WithEnvironmentVariable("MY_APP_ENV"));
```

---

## Advanced Configuration Options

### Customize Configuration Path

Use Kedro-style `conf/` directory:

```csharp
builder.UseConfiguration(config => config
  .WithConfigurationPath("conf"));
```

### Change Configuration File Name

Use `parameters.json` instead of `appsettings.json`:

```csharp
builder.UseConfiguration(config => config
  .WithConfigurationFileName("parameters"));
```

This will load: `parameters.json`, `parameters.{Environment}.json`, `parameters.Local.json`

### YAML Support

YAML files are supported automatically via NetEscapades.Configuration.Yaml:

```yaml
# appsettings.yml
DataScience:
  ModelParams:
    TestSize: 0.2
    RandomState: 3
```

Both JSON and YAML files can coexist. To disable YAML:

```csharp
builder.UseConfiguration(config => config
  .WithYamlSupport(false));
```

---

## Parameter Validation with DataAnnotations

Add validation attributes to your parameter classes:

```csharp
using System.ComponentModel.DataAnnotations;

public record ModelParams {
  [Range(0.0, 1.0, ErrorMessage = "TestSize must be between 0.0 and 1.0")]
  public double TestSize { get; init; } = 0.2;

  [Range(0, int.MaxValue, ErrorMessage = "RandomState must be non-negative")]
  public int RandomState { get; init; } = 3;
}
```

Validation occurs at application startup. If validation fails, you'll see:

```
System.ComponentModel.DataAnnotations.ValidationException:
Configuration validation failed for 'DataScience:ModelParams':
  - TestSize must be between 0.0 and 1.0
```

---

## Migration Guide: Inline → Configuration

### Before (Inline Parameters)

```csharp
builder
  .RegisterPipeline<MyCatalog, ModelParams>(
    label: "DataScience",
    creator: DataSciencePipeline.Create,
    parameters: new ModelParams {
      TestSize = 0.2,
      RandomState = 3
    }
  )
  .WithDescription("Trains ML model");
```

### After (Configuration-Based)

**1. Enable configuration:**
```csharp
builder.UseConfiguration();
```

**2. Create appsettings.json:**
```json
{
  "DataScience": {
    "ModelParams": {
      "TestSize": 0.2,
      "RandomState": 3
    }
  }
}
```

**3. Update registration:**
```csharp
builder
  .RegisterPipelineWithConfiguration<MyCatalog, ModelParams>(
    label: "DataScience",
    creator: DataSciencePipeline.Create,
    configurationSection: "DataScience:ModelParams"
  )
  .WithDescription("Trains ML model");
```

**4. Configure .csproj to copy files:**
```xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="appsettings.Development.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="appsettings.Local.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

**5. Add to .gitignore:**
```
appsettings.Local.json
```

---

## When to Use Configuration vs. Inline Parameters

### Use Configuration Files When:
- ✅ Parameters change between environments (dev/staging/prod)
- ✅ Non-developers need to adjust settings
- ✅ You want Kedro-like configuration management
- ✅ Parameters contain sensitive data (use appsettings.Local.json)
- ✅ Team collaboration requires shared base configuration

### Use Inline Parameters When:
- ✅ Parameters are truly constant across all environments
- ✅ Parameters are computed at runtime (not static values)
- ✅ You prefer explicit, compile-time configuration
- ✅ Simple applications with few parameters

---

## Complete Example

See `tests/Flowthru.Tests.KedroSpaceflights` for a working example that demonstrates:
- Configuration file structure (appsettings.json, appsettings.Development.json)
- DataAnnotations validation
- Both inline and configuration-based registration
- Environment-specific overrides

Run with different environments:
```bash
# Use base configuration (Production)
dotnet run DataScience

# Use Development overrides
FLOWTHRU_ENV=Development dotnet run DataScience
```

---

## Troubleshooting

### Configuration file not found
```
FileNotFoundException: Could not find file '.../appsettings.json'
```

**Solution:** Ensure the file exists and is copied to the output directory:
```xml
<None Update="appsettings.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

### Configuration section not found
```
InvalidOperationException: Configuration section 'DataScience' not found
```

**Solution:** Check the section path matches your JSON structure:
```json
{
  "DataScience": {  ← This is the section path
    "ModelParams": { ... }
  }
}
```

### Validation errors
```
ValidationException: Configuration validation failed for 'DataScience:ModelParams':
  - TestSize must be between 0.0 and 1.0
```

**Solution:** Fix the invalid values in your configuration file or adjust validation attributes on your parameter class.

### Parameter type missing parameterless constructor
```
CompileError: 'DataSciencePipelineParams' must have a public parameterless constructor
```

**Solution:** Ensure parameter records use property syntax (not primary constructor):

❌ **Don't use:**
```csharp
public record ModelParams(double TestSize, int RandomState);
```

✅ **Use instead:**
```csharp
public record ModelParams {
  public double TestSize { get; init; } = 0.2;
  public int RandomState { get; init; } = 3;
}
```

---

## See Also

- [How to Register Pipelines with Parameters](pipeline-parameters.md) - Inline parameter registration
- [How to Structure Your Project](project-structure.md) - Project organization best practices
- Microsoft.Extensions.Configuration documentation
