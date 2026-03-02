# Set Up Data

This page explains how to add datasets to your project and register them in Flowthru's Data Catalog. You'll define schemas for each dataset and create catalog entries that connect schemas to physical files.

## Project Datasets

The Spaceflights project uses three datasets:

- **`companies.csv`** - Space shuttle companies (location, fleet size, rating)
- **`reviews.csv`** - Customer reviews (comfort, cleanliness, price ratings)
- **`shuttles.xlsx`** - Spacecraft attributes (engine type, passenger capacity)

These files represent different formats you might encounter in real projects: CSV and Excel.

## Download the Data

First, start by downloading these three files. You can [find and download them](https://github.com/kedro-org/kedro-starters/tree/main/spaceflights-pandas/%7B%7B%20cookiecutter.repo_name%20%7D%7D/data/01_raw) from the Kedro Spaceflights starter.

Once you have downloaded them, put them into your Data directory at:

```
Spaceflights
├── Data/                      # Data catalog and schemas
    ├── _01_Raw/
        ├── Datasets/
            ├── companies.csv
            ├── reviews.csv
            ├── shuttles.xlsx
```

## Define Schemas

Great! We have some data — now, what do we do with it?

The first thing we need to do is take a look at the **schemas** of our data. Schemas are our way of keeping track, for each dataset:

1. What columns do we have? and
2. What type of data are in those columns?

Taking a look at the data, we have:

**companies.csv:**

| Column Name      | Data Type                 |
| ---------------- | ------------------------- |
| id               | ID as text                |
| company_rating   | Rating percentage as text |
| iata_approved    | Boolean flag as text      |
| company_location | Location as text          |

**reviews.csv:**

| Column Name          | Data Type            |
| -------------------- | -------------------- |
| id                   | ID as text           |
| company_id           | Company ID as text   |
| review_scores_rating | Rating score as text |

**shuttles.xlsx:**

| Column Name             | Data Type               |
| ----------------------- | ----------------------- |
| id                      | ID as text              |
| shuttle_location        | Location as text        |
| shuttle_type            | Type as text            |
| engine_type             | Engine type as text     |
| passenger_capacity      | Capacity number as text |
| crew                    | Crew count as text      |
| d_check_complete        | Boolean flag as text    |
| moon_clearance_complete | Boolean flag as text    |
| price                   | Price as text           |
| company_id              | Company ID as text      |

Notice that all fields are text — even numbers, percentages, and boolean flags. This is typical of raw data imports from CSV and Excel files. For now, our schemas will reflect that this is all text that needs to be processed.

### Company Schema

We'll go through the Company table schema step-by-step, as a reference for how all of the other schemas for this pipeline will be written.

First, we need to create the new schema file. In each layer, there is a `Schemas` directory. That's where we'll create the schemas for data in this layer. In the case of the `companies.csv`, we'll put it in the raw schemas:

```
Spaceflights
├── Data/                      # Data catalog and schemas
    ├── _01_Raw/
        ├── Schemas/
            ├── CompanySchema.cs # New CompanySchema.cs file
```

Now that we have the file, we'll fill it in with the schema. For companies, the schema will look like:

```csharp
using Flowthru.Abstractions;

namespace Spaceflights.Data._01_Raw.Schemas;

[FlowthruSchema]
public partial record CompanySchema
{
  [SerializedLabel("id")]
  public required string Id { get; init; }

  [SerializedLabel("company_rating")]
  public required string CompanyRating { get; init; }

  [SerializedLabel("iata_approved")]
  public required string IataApproved { get; init; }

  [SerializedLabel("company_location")]
  public required string CompanyLocation { get; init; }
}
```

Let's go through what we've done here, step by step.

1. `record CompanySchema` defines how we reference data that has this shape in other places in the application. Whenever you're working with any data that has these columns, and these data types, we can say that data is a piece of `CompanySchema` data.
2. In your code, you may not always want to reference a column by its name in the file. After all, you're not working with CSVs by hand — you're using C#! The `[SerializedLabel]` lets us define what column we're referencing in the original file.
3. For each column, we define the **type** of that column, as well as how we'll reference the column in our code later. `string CompanyRating` C#, and our future selves writing nodes, that CompanyRating will be a string.

### Review & Shuttles Schemas

We'll take the process we did for the CompanySchema and repeat it for the Reviews and Shuttles tables, creating:

- `Data/_01_Raw/Schemas/ReviewSchema.cs` and
- `Data/_01_Raw/Schemas/ShuttleSchema.cs` and

You can find these schemas online in the [Flowthru Spaceflights starter code](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/Spaceflights/Data/_01_Raw/Schemas).

## Create Catalog Entries

We now have the **data** (the tables we downloaded earlier) and the **schemas** — what those tables actually look like.

With those two pieces of information, we can now create **Data Catalog** entries for these tables! The purpose of data catalog is to bring together three pieces of information necessary to work with your data:

1. **How** the data is shaped — the *schema*
2. **What format** it's stored in (CSV, Parquet, Excel)
3. **Where** the data lives

For raw data, we'll put our Catalog entries into the file at:

```
Spaceflights
├── Data/                      # Data catalog and schemas
    ├── _01_Raw/
        ├── Catalog.Raw.cs     # The Data Catalog for raw data
```

This file will already have a CSV catalog entry from the minimal starter. We'll be creating three catalog entries that look very similar to that one. The pattern for Catalog entries is:

```csharp
using Flowthru.Data;
using Spaceflights.Data._01_Raw.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{
  /// ...

  public ICatalogEntry<IEnumerable<__SCHEMA__>> __NAME__ =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.__FORMAT__<__SCHEMA__>(
          label: "__NAME__",
          path: "__PATH__"
        )
    );
  
  /// ...
}
```

Let's break down what information we need, and where to put it:

1. In the `__NAME__` sections, we'll put the name of the data entry, as we want to reference it in our code, later
2. In the `__SCHEMA__` sections, we'll use the name of the schema we created for the file
3. In the `__FORMAT__` section, we'll say how our data is stored — in this case, CSV
4. In the `__PATH__` section, we'll state where, in our project, the data is located.

For our CompanySchema then:

1. For the name, we'll simply name it Companies. Straightforward enough, right?
2. For the schema, we'll use our CompanySchema that we created earlier.
3. For the format, this data is stored as a CSV
4. And, for the path, the data is located at `/Data/_01_Raw/Datasets/companies.csv`.

Our entry for Companies, then, will look like:

```cs
  public ICatalogEntry<IEnumerable<CompanySchema>> Companies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CompanySchema>(
          label: "Companies",
          path: $"{basePath}/Data/_01_Raw/Datasets/companies.csv"
        )
    );
```

We can use this pattern to add the three new entries for our three input tables:

```cs
using Flowthru.Data;
using Spaceflights.Data._01_Raw.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{

  // Minimal "Names" Entry — keep it for now, to keep the project building.

  public ICatalogEntry<IEnumerable<CompanySchema>> Companies =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<CompanySchema>(
          label: "Companies",
          filePath: $"{_basePath}/_01_Raw/Datasets/companies.csv"
        )
    );

  public ICatalogEntry<IEnumerable<ReviewSchema>> Reviews =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ReviewSchema>(
          label: "Reviews",
          filePath: $"{_basePath}/_01_Raw/Datasets/reviews.csv"
        )
    );

  public ICatalogEntry<IEnumerable<ShuttleSchema>> Shuttles =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Excel<ShuttleSchema>(
          label: "Shuttles",
          filePath: $"{_basePath}/_01_Raw/Datasets/shuttles.xlsx",
          sheetName: "Sheet1"
        )
    );
}
```


## What's Next?

Alright! We've got some Catalog entries: now what?

### Check your work!

At this point, your schemas and Catalog Entries should be correctly set up. You can confirm this by building the project:

```bash
dotnet build
```

If you have zero build issues: congratulations! We're ready to move onto our next steps: Defining new nodes!

You've defined schemas and registered raw datasets in the catalog. Next, you'll create a pipeline that processes this data into a format ready for data science!

**Continue to: [Create a Pipeline](03-create-a-pipeline.md)**
