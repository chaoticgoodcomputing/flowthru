# KedroIris Starter

A Flowthru starter project demonstrating multi-class classification on the Iris dataset. This example showcases the complete 8-layer Kedro data engineering convention adapted for type-safe .NET development.

## Overview

This starter trains a multi-class logistic regression model to classify iris flowers into three species (setosa, versicolor, virginica) based on sepal and petal measurements. The implementation follows the architecture from [Kedro's Iris starter](https://github.com/kedro-org/kedro-starters/tree/main/astro-airflow-iris) while demonstrating Flowthru's type-safe approach.

## Project Structure

```
KedroIris/
├── Data/                           # 8-layer data organization
│   ├── _01_Raw/                   # Immutable source data
│   │   └── Datasets/iris.csv
│   ├── _02_Intermediate/          # Typed raw data (unused but available)
│   ├── _03_Primary/               # Domain models (unused but available)
│   ├── _04_Feature/               # Engineered features
│   ├── _05_ModelInput/            # Train/test splits
│   ├── _06_Models/                # Serialized models
│   ├── _07_ModelOutput/           # Predictions
│   └── _08_Reporting/             # Metrics and visualizations
├── Pipelines/
│   ├── DataEngineering/           # Data splitting and encoding
│   └── DataScience/               # Model training and evaluation
└── Program.cs                      # Application entry point
```

## Pipelines

### DataEngineering

Prepares the dataset for model training:
- Loads raw iris measurements and species labels
- Applies one-hot encoding to species classifications
- Shuffles and splits data into training (80%) and test (20%) sets
- Separates features (X) from labels (Y)

**Outputs:**
- `_04_Feature/iris_features.csv` - One-hot encoded dataset
- `_05_ModelInput/train_x.csv` - Training features
- `_05_ModelInput/train_y.csv` - Training labels
- `_05_ModelInput/test_x.csv` - Test features
- `_05_ModelInput/test_y.csv` - Test labels

### DataScience

Trains and evaluates a classification model:
- Trains multi-class logistic regression using gradient descent
- Predicts species for test samples
- Evaluates accuracy and saves metrics

**Outputs:**
- `_06_Models/iris_model.json` - Trained model weights
- `_07_ModelOutput/predictions.csv` - Species predictions
- `_08_Reporting/metrics.json` - Accuracy metrics

## Running the Starter

```bash
# Run data engineering pipeline (splits and encodes data)
dotnet run -- DataEngineering

# Run data science pipeline (trains model and evaluates)
dotnet run -- DataScience
```

## Configuration

Edit `appsettings.json` to adjust pipeline parameters:

```json
{
  "Flowthru": {
    "Pipelines": {
      "DataEngineering": {
        "TestDataRatio": 0.2      // Proportion of data for testing
      },
      "DataScience": {
        "NumTrainIter": 10000,     // Training iterations
        "LearningRate": 0.01       // Gradient descent step size
      }
    }
  }
}
```

## The 8-Layer Convention

Even though this simple example only actively uses 5 of the 8 layers, all layers are present with catalog partials and schema directories. This demonstrates the complete Kedro convention for downstream users to "grow into":

| Layer              | Purpose                     | Used in Iris?            |
| ------------------ | --------------------------- | ------------------------ |
| `_01_Raw`          | Immutable source data       | ✓ Yes - iris.csv         |
| `_02_Intermediate` | Typed representation of raw | ✗ Template only          |
| `_03_Primary`      | Domain-specific models      | ✗ Template only          |
| `_04_Feature`      | Engineered features         | ✓ Yes - one-hot encoded  |
| `_05_ModelInput`   | Training/test splits        | ✓ Yes - X/Y splits       |
| `_06_Models`       | Serialized trained models   | ✓ Yes - model weights    |
| `_07_ModelOutput`  | Predictions and scores      | ✓ Yes - predictions      |
| `_08_Reporting`    | Metrics and visualizations  | ✓ Yes - accuracy metrics |

## Expected Results

With default parameters, the model typically achieves **90-95% accuracy** on the test set. The simple logistic regression approach works well for the linearly separable Iris dataset.

## Key Patterns Demonstrated

1. **Layered Data Organization** - All 8 Kedro layers with numbered prefixes
2. **Partial Catalog Classes** - One file per layer for maintainability
3. **Type-Safe Schemas** - `[FlowthruSchema]` attribute with source generation
4. **Static Node Factories** - Pure functions returning typed transforms
5. **Configuration-Driven Pipelines** - Parameters injected from appsettings.json
6. **Multi-Output Nodes** - Tuple returns for splitting operations

## Next Steps

- Experiment with different test ratios and hyperparameters
- Add a third pipeline for hyperparameter tuning
- Implement cross-validation in the DataScience pipeline
- Add confusion matrix visualization to the Reporting layer
- Try different models (e.g., decision trees, neural networks)

## Resources

- [Flowthru Documentation](../../../docs/)
- [Kedro Data Engineering Layers](https://towardsdatascience.com/the-importance-of-layered-thinking-in-data-engineering-a09f685edc71)
- [UCI Iris Dataset](https://archive.ics.uci.edu/ml/datasets/iris)
