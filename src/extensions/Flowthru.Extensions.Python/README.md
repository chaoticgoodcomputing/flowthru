# Flowthru.Extensions.Python

Python node integration for Flowthru data pipelines. Write data transformation logic in Python while preserving Flowthru's type-safe compile-time and pre-flight validation guarantees.

## Status

**Phase 1 (Current):** Runtime infrastructure and service integration  
**Phase 2-6:** Coming soon — see `/docs/scratch/python-node-extension-design.md`

## Installation

```bash
dotnet add package Flowthru.Extensions.Python
```

## Quick Start

```csharp
services.AddFlowthru(flowthru =>
{
    flowthru
        .RegisterCatalog<MyCatalog>()
        .UsePython(python =>
        {
            python.VenvPath = ".venv";
            python.ModuleSearchPaths.Add("Pipelines");
        })
        .RegisterPipelines(catalog => new Dictionary<string, Pipeline>
        {
            ["my_pipeline"] = MyPipeline.Create(catalog)
        });
});
```

## Requirements

**Flowthru uses [`uv`](https://docs.astral.sh/uv/) to manage Python environments.** Install it with:

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh  # macOS/Linux
# or
brew install uv  # macOS with Homebrew
```

Initialize your Python environment in the project root:

```bash
uv venv                          # Create .venv/
uv pip install pandas pyarrow    # Install data dependencies
```

Run your Flowthru application with `uv run` to automatically configure the Python runtime:

```bash
uv run dotnet run
```

### Alternative: Manual VIRTUAL_ENV

If not using `uv`, activate a virtual environment before running:

```bash
python -m venv .venv
source .venv/bin/activate  # Linux/macOS
# or
.venv\Scripts\activate      # Windows

dotnet run
```

## Features

### Phase 1: Runtime Foundation
- ✅ Python runtime lifecycle management
- ✅ Service integration via `UsePython()`
- ✅ Abstracted execution model (`IPythonExecutor`)
- ✅ `uv`-based Python environment detection via `VIRTUAL_ENV`
- ✅ Automatic venv discovery through `pyvenv.cfg` parsing

### Coming Soon
- Phase 2: Scalar and 1×1 node registration
- Phase 3: Arrow-based tabular data marshalling
- Phase 4: Pre-flight validation (schema contracts, dry-run)
- Phase 5: N×M node overloads and Python schema generation
- Phase 6: Example projects and stress testing

## Architecture

See `/docs/scratch/python-node-extension-design.md` for detailed design rationale.

**Key Principles:**
- **Type safety at the boundary:** Python functions are black boxes, but their wrappers are typed
- **Fail-fast validation:** Schema mismatches caught at pre-flight, not runtime
- **Convention with escape hatches:** Auto-detect `.venv/`, but allow explicit config

## License

Apache-2.0
