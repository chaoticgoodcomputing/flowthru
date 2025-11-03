# Flowthru.Tests.Examples

Integration test suite that executes all example projects to verify they run successfully and provide code coverage for the Flowthru framework.

## Purpose

This test project serves multiple purposes:

1. **Integration Testing**: Verifies that example projects execute successfully as real-world usage scenarios
2. **Code Coverage**: Measures which framework code paths are exercised by actual example implementations
3. **Regression Detection**: Ensures framework changes don't break existing examples
4. **Documentation Validation**: Confirms that example projects remain functional as living documentation

## Architecture

### Auto-Discovery

The test suite automatically discovers example projects from the `examples/` directory using file system scanning. This means:

- **No manual test file creation**: New examples are automatically included
- **Zero maintenance overhead**: Adding a new example requires only updating the `.csproj`
- **Consistent testing**: All examples are tested the same way via NUnit's `TestCaseSource`

### Programmatic Execution

Examples are executed programmatically by invoking their `Main` methods via reflection:

- **Code Coverage**: Example code paths are measured by `coverlet.collector`
- **Type Safety**: Compile-time checking ensures referenced examples exist
- **Performance**: Faster than subprocess execution, no process overhead
- **Framework Integration**: Examples execute within the test process context

### Test Structure

```
tests/Flowthru.Tests.Examples/
├── _Infrastructure/              # Discovery and execution infrastructure
│   ├── ExampleDiscovery.cs      # Scans examples/ directory for projects
│   ├── ExampleProject.cs        # Model for discovered example metadata
│   ├── ExampleTestRunner.cs     # Executes examples via reflection
│   └── ExampleTestResult.cs     # Result model with exit code and timing
└── ExampleIntegrationTests.cs   # NUnit test fixture with auto-discovery
```

## Running Tests

### All Example Tests

```bash
# Via Nx (recommended)
nx run test/examples:test

# Via dotnet CLI
cd tests/Flowthru.Tests.Examples
dotnet test
```

### Discovery Tests Only

Verify that all examples are discovered correctly:

```bash
nx run test/examples:test:discovery
```

### With Code Coverage

```bash
# Collect coverage data (cleans TestResults first for accurate results)
nx run test/examples:coverage

# Generate HTML report from latest coverage run
nx run test/examples:coverage:html

# Open the HTML report in your browser (runs coverage + html generation)
nx run test/examples:coverage:open

# Alternative: Keep historical coverage data
nx run test/examples:coverage:keep-history
```

**How it works:**
- `coverage` automatically removes old `TestResults/` before running tests
- Coverage data is generated in `TestResults/<guid>/` with a unique identifier per run
- `coverage:html` finds the most recent coverage file and generates HTML report
- HTML report is always written to `TestResults/html/index.html` (overwrites previous)

Coverage reports are generated in multiple formats:
- `coverage.cobertura.xml` - Standard Cobertura XML (compatible with most CI/CD platforms)
- `coverage.json` - JSON format (for programmatic analysis and custom tooling)
- `coverage.info` - LCOV format (for SonarQube, Codecov, and other tools)
- `TestResults/html/` - Interactive HTML report with file-by-file breakdown

## Test Categories

Tests are organized with NUnit categories:

- `[Category("Examples")]` - All example integration tests
- `[Category("Integration")]` - Marks these as integration tests (may be slower)

Filter by category:

```bash
dotnet test --filter "Category=Examples"
dotnet test --filter "Category=Integration"
```

## Adding New Examples

When you add a new example project to `examples/`:

1. **Add ProjectReference** in `Flowthru.Tests.Examples.csproj`:
   ```xml
   <ProjectReference Include="..\..\examples\YourNewExample\YourNewExample.csproj" />
   ```

2. **Run discovery test** to verify:
   ```bash
   nx run test/examples:test:discovery
   ```

3. **Run all tests**:
   ```bash
   nx run test/examples:test
   ```

That's it! The example will be automatically discovered and tested.

## How It Works

### Discovery Process

1. **File System Scan**: `ExampleDiscovery` scans the `examples/` directory for `.csproj` files
2. **Executable Filter**: Only projects with `<OutputType>Exe</OutputType>` are included
3. **Entry Point Resolution**: Finds the `Program` type in each example's assembly
4. **Metadata Collection**: Captures project name, paths, and entry point type

### Execution Process

1. **Reflection Invocation**: `ExampleTestRunner` locates the `Main` method via reflection
2. **Signature Detection**: Supports multiple Main signatures:
   - `void Main()`
   - `int Main()`
   - `Task Main()`
   - `Task<int> Main()`
   - `void Main(string[] args)`
   - `int Main(string[] args)`
   - `Task Main(string[] args)`
   - `Task<int> Main(string[] args)`
3. **Async Handling**: Awaits async Main methods and captures exit codes
4. **Error Handling**: Captures exceptions and converts to test failures

### Coverage Collection

Because examples are `<ProjectReference>` dependencies, `coverlet.collector` automatically:

- Instruments example assemblies during test execution
- Tracks which code paths in Flowthru framework are exercised
- Generates coverage reports including example execution paths

## Test Assertions

Each example is verified with three assertions:

1. **No Exception**: The example should not throw unhandled exceptions
2. **Exit Code Zero**: The Main method should return 0 (or complete successfully)
3. **Success Flag**: The combined result should indicate success
