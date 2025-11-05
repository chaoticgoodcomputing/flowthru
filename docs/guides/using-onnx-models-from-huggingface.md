# Using ONNX Models from HuggingFace

This guide explains how to download and use pre-trained ONNX models from HuggingFace for use in Flowthru pipelines.

## Overview

ONNX (Open Neural Network Exchange) is a standard format for representing machine learning models. Flowthru treats ONNX models as **first-class data catalog entries**, similar to CSV files or Parquet datasets.

### Why ONNX?

- **Interoperability**: Models trained in PyTorch, TensorFlow, or other frameworks can be converted to ONNX
- **Performance**: ONNX Runtime provides optimized inference across platforms
- **Portability**: Same model file works on Windows, macOS, and Linux
- **ML.NET Integration**: Native support in ML.NET via `Microsoft.ML.OnnxRuntime`

## Prerequisites

- Python 3.8+ (for downloading/converting models)
- `pip` or `conda` package manager
- HuggingFace account (free, for downloading models)

## Method 1: Using Optimum CLI (Recommended)

The easiest way to obtain ONNX models from HuggingFace is using the `optimum` library.

### Step 1: Install Optimum

```bash
pip install optimum[onnxruntime]
```

### Step 2: Download BERT Model

For the MagicAtlas example (BERT-base-uncased):

```bash
# Create export directory
mkdir -p /tmp/bert-base-onnx

# Export BERT model to ONNX format
optimum-cli export onnx \
  --model bert-base-uncased \
  --task feature-extraction \
  /tmp/bert-base-onnx/
```

This downloads the model and converts it to ONNX format in one command.

### Step 3: Copy to Flowthru Project

```bash
# For MagicAtlas project
cp /tmp/bert-base-onnx/model.onnx \
   examples/MagicAtlas/Data/_04_Embeddings/Models/bert-base-uncased.onnx
```

## Method 2: Manual Download from HuggingFace

Some models are already available in ONNX format on HuggingFace.

### Step 1: Find ONNX Model

Visit [HuggingFace Models](https://huggingface.co/models) and search for:
- `onnx` in the model card
- Models in the `onnx` organization (e.g., `onnx/bert-base-uncased`)

### Step 2: Download Model File

```bash
# Install HuggingFace CLI
pip install huggingface_hub

# Download model
huggingface-cli download \
  onnx/bert-base-uncased \
  --include "*.onnx" \
  --local-dir /tmp/bert-onnx
```

### Step 3: Copy to Project

```bash
cp /tmp/bert-onnx/*.onnx \
   examples/MagicAtlas/Data/_04_Embeddings/Models/bert-base-uncased.onnx
```

## Method 3: Convert PyTorch Model to ONNX

If you have a PyTorch model, you can convert it manually.

### Step 1: Install Dependencies

```bash
pip install torch transformers onnx
```

### Step 2: Convert Model

```python
import torch
from transformers import AutoModel, AutoTokenizer

# Load model
model_name = "bert-base-uncased"
model = AutoModel.from_pretrained(model_name)
tokenizer = AutoTokenizer.from_pretrained(model_name)

# Create dummy input
dummy_input = tokenizer("Example text", return_tensors="pt")

# Export to ONNX
torch.onnx.export(
    model,
    (dummy_input["input_ids"], dummy_input["attention_mask"]),
    "/tmp/bert-base-uncased.onnx",
    input_names=["input_ids", "attention_mask"],
    output_names=["last_hidden_state"],
    dynamic_axes={
        "input_ids": {0: "batch", 1: "sequence"},
        "attention_mask": {0: "batch", 1: "sequence"},
        "last_hidden_state": {0: "batch", 1: "sequence"}
    },
    opset_version=14
)
```

### Step 3: Copy to Project

```bash
cp /tmp/bert-base-uncased.onnx \
   examples/MagicAtlas/Data/_04_Embeddings/Models/
```

## Verifying the ONNX Model

Once placed in your project, Flowthru will automatically validate the model file during pipeline execution.

### Validation Checks

Flowthru's `OnnxModelStorageAdapter` performs **shallow inspection**:

1. ✅ File exists at specified path
2. ✅ File has `.onnx` extension
3. ✅ File is readable and non-empty
4. ✅ File size > 0 bytes

### Test Validation

Run your pipeline to verify the model loads successfully:

```bash
cd examples/MagicAtlas
dotnet run -- AtlasAnalysis
```

Expected output:

```
info: → Validating external data sources...
info: All external inputs passed validation
info: ✓ 2 external data sources validated
```

## Model Placement in Flowthru Projects

ONNX models should be placed in the appropriate data layer directory:

### Recommended Structure

```
YourProject/
├── Data/
│   ├── _04_Embeddings/  (or appropriate layer)
│   │   ├── Models/
│   │   │   ├── bert-base-uncased.onnx
│   │   │   ├── roberta-large.onnx
│   │   │   └── .gitkeep
│   │   ├── Datasets/
│   │   │   └── embeddings.parquet
│   │   └── Schemas/
│   │       └── EmbeddingSchema.cs
```

### .gitignore Considerations

ONNX models are often large (100MB - 1GB+). Consider adding to `.gitignore`:

```gitignore
# ONNX Models (download separately)
**/_04_Embeddings/Models/*.onnx
!**/_04_Embeddings/Models/.gitkeep
```

Include a README in the Models directory with download instructions:

```markdown
# Models Directory

This directory contains ONNX models for inference.

## Required Models

- `bert-base-uncased.onnx` - BERT base model (110MB)
  - Download: See docs/guides/using-onnx-models-from-huggingface.md
  - Source: https://huggingface.co/bert-base-uncased
```

## Common Models and Sizes

| Model                | Size   | Use Case                  | Download Command                                                         |
| -------------------- | ------ | ------------------------- | ------------------------------------------------------------------------ |
| BERT-base            | ~110MB | Text embeddings           | `optimum-cli export onnx --model bert-base-uncased`                      |
| BERT-large           | ~340MB | Higher quality embeddings | `optimum-cli export onnx --model bert-large-uncased`                     |
| DistilBERT           | ~66MB  | Faster inference          | `optimum-cli export onnx --model distilbert-base-uncased`                |
| RoBERTa              | ~125MB | Improved BERT             | `optimum-cli export onnx --model roberta-base`                           |
| SentenceTransformers | ~90MB  | Semantic search           | `optimum-cli export onnx --model sentence-transformers/all-MiniLM-L6-v2` |

## Troubleshooting

### Error: "ONNX model file not found"

**Cause**: Model file doesn't exist at specified path

**Solution**:
1. Verify file path in your catalog entry
2. Check file actually exists: `ls -lh Data/_04_Embeddings/Models/`
3. Ensure file has `.onnx` extension

### Error: "File does not have .onnx extension"

**Cause**: File has wrong extension (e.g., `.bin`, `.pt`)

**Solution**:
1. Rename file to `.onnx` extension
2. Or convert model to ONNX format first

### Error: "ONNX model file is empty"

**Cause**: Download failed or file corrupted

**Solution**:
1. Re-download the model
2. Verify file size: `ls -lh <model_file>`
3. Compare with expected size on HuggingFace

### Error: "Cannot load ONNX model"

**Cause**: Model format incompatible with ONNX Runtime version

**Solution**:
1. Check ONNX opset version compatibility
2. Update `Microsoft.ML.OnnxRuntime` package
3. Re-export model with compatible opset version

## Next Steps

After placing your ONNX model:

1. **Update Catalog Entry**: Ensure file path matches in `Catalog.Embeddings.cs`
2. **Implement Inference**: Update your node to use actual ONNX inference (currently using placeholders)
3. **Test Pipeline**: Run `dotnet run -- YourPipeline` to verify end-to-end flow
4. **Optimize Performance**: Consider batching, caching, or GPU acceleration for large datasets

## Additional Resources

- [ONNX Official Site](https://onnx.ai/)
- [HuggingFace ONNX Models](https://huggingface.co/models?library=onnx)
- [Optimum Documentation](https://huggingface.co/docs/optimum/index)
- [Microsoft ML.NET ONNX Guide](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx)
- [ONNX Runtime Documentation](https://onnxruntime.ai/)

---

**Questions or Issues?** Open an issue on the Flowthru GitHub repository.
