---
title: Set Up Data
description: Add the Spaceflights datasets to your project, define a schema for each, and register catalog items that connect those schemas to the physical CSV and Excel files.
review: draft
---

This page explains how to add datasets to your project and register them in Flowthru's Catalog. You'll define schemas for each dataset and create catalog items that connect schemas to physical files.

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

<!-- flowthru:snippet:docs:schema-company:start -->
```csharp
using Flowthru.Data.Schema;

namespace Spaceflights.Data._01_Raw.Schemas;

/// <summary>
/// Represents raw company data as imported from text files.
/// All fields are stored as strings pending parsing.
/// </summary>
[FlowthruSchema]
public partial record CompanySchema
{
  /// <summary>
  /// Unique identifier for the company.
  /// </summary>
  [SerializedLabel("id")]
  public string Id { get; init; } = null!;

  /// <summary>
  /// Company rating as a percentage string (e.g., "90%").
  /// </summary>
  [SerializedLabel("company_rating")]
  public string CompanyRating { get; init; } = null!;

  /// <summary>
  /// IATA approval status as a string flag ("t" for true, "f" for false).
  /// </summary>
  [SerializedLabel("iata_approved")]
  public string IataApproved { get; init; } = null!;

  /// <summary>
  /// Geographic location of the company.
  /// </summary>
  [SerializedLabel("company_location")]
  public string CompanyLocation { get; init; } = null!;
}
```
<!-- flowthru:snippet:docs:schema-company:end -->

Let's go through what we've done here, step by step.

1. `record CompanySchema` defines how we reference data that has this shape in other places in the application. Whenever you're working with any data that has these columns, and these data types, we can say that data is a piece of `CompanySchema` data.
2. In your code, you may not always want to reference a column by its name in the file. After all, you're not working with CSVs by hand — you're using C#! The `[SerializedLabel]` attribute lets us define which column in the original file each property maps to.
3. For each column, we declare its **type** and the name we'll use to reference it in our code later. `public string CompanyRating` tells C#, and our future selves writing Steps, that `CompanyRating` is a string. (The `= null!` initializer tells the compiler these required fields are populated during deserialization.)

### Review & Shuttles Schemas

We'll take the process we did for the CompanySchema and repeat it for the Reviews and Shuttles tables, creating:

- `Data/_01_Raw/Schemas/ReviewSchema.cs` and
- `Data/_01_Raw/Schemas/ShuttleSchema.cs` and

You can find these schemas online in the [Flowthru Spaceflights starter code](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples/starter/Spaceflights/Data/_01_Raw/Schemas).

## Create Catalog Items

We now have the **data** (the tables we downloaded earlier) and the **schemas** — what those tables actually look like.

With those two pieces of information, we can now create **Catalog** items for these tables! The purpose of the catalog is to bring together three pieces of information necessary to work with your data:

1. **How** the data is shaped — the *schema*
2. **What format** it's stored in (CSV, Parquet, Excel)
3. **Where** the data lives

For raw data, we'll put our catalog items into the file at:

```
Spaceflights
├── Data/                      # Data catalog and schemas
    ├── _01_Raw/
        ├── Catalog.Raw.cs     # The Catalog for raw data
```

This file will already have a CSV catalog item from the minimal starter. We'll be creating three catalog items that look very similar to that one. The pattern for catalog items is:

```csharp
using Flowthru.Data;
using Spaceflights.Data._01_Raw.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{
  /// ...

  public IItem<IEnumerable<__SCHEMA__>> __NAME__ =>
    CreateItem(() => Item.Of<IEnumerable<__SCHEMA__>>("__NAME__")
      .__FORMAT__()
      .AtPath("__PATH__")
      .Build());
  
  /// ...
}
```

Let's break down what information we need, and where to put it:

1. In the `__NAME__` sections, we'll put the name of the catalog item, as we want to reference it in our code, later
2. In the `__SCHEMA__` sections, we'll use the name of the schema we created for the file
3. In the `__FORMAT__` section, we'll say how our data is stored — in this case, CSV
4. In the `__PATH__` section, we'll state where, in our project, the data is located.

For our CompanySchema then:

1. For the name, we'll simply name it Companies. Straightforward enough, right?
2. For the schema, we'll use our CompanySchema that we created earlier.
3. For the format, this data is stored as a CSV
4. And, for the path, the data is located at `/Data/_01_Raw/Datasets/companies.csv`.

Our entry for Companies, then, will look like:

<!-- flowthru:snippet:docs:catalog-raw-companies:start -->
```csharp
/// <summary>Raw company data imported from external sources.</summary>
public IItem<IEnumerable<CompanySchema>> Companies =>
  CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
    .Csv()
    .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
    .Build());
```
<!-- flowthru:snippet:docs:catalog-raw-companies:end -->

We use the same pattern to add all three entries for our input tables. Note that the Excel item adds one extra call, `.WithSheet("Sheet1")`, to pick the worksheet:

<!-- flowthru:snippet:docs:catalog-raw-all:start -->
```csharp
using Flowthru.Data.Catalog;
using Spaceflights.Data._01_Raw.Schemas;

namespace Spaceflights.Data;

public partial class Catalog
{
  /// <summary>Raw company data imported from external sources.</summary>
  public IItem<IEnumerable<CompanySchema>> Companies =>
    CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
      .Build());

  /// <summary>Raw review data imported from external sources.</summary>
  public IItem<IEnumerable<ReviewSchema>> Reviews =>
    CreateItem(() => Item.Of<IEnumerable<ReviewSchema>>("Reviews")
      .Csv()
      .AtPath($"{_basePath}/_01_Raw/Datasets/reviews.csv")
      .Build());

  /// <summary>Raw shuttle data imported from external sources (Excel).</summary>
  public IItem<IEnumerable<ShuttleSchema>> Shuttles =>
    CreateItem(() => Item.Of<IEnumerable<ShuttleSchema>>("Shuttles")
      .Excel()
      .AtPath($"{_basePath}/_01_Raw/Datasets/shuttles.xlsx")
      .WithSheet("Sheet1")
      .Build());
}
```
<!-- flowthru:snippet:docs:catalog-raw-all:end -->


## What's Next?

Alright! We've got some Catalog entries: now what?

### Check your work!

At this point, your schemas and Catalog Entries should be correctly set up. You can confirm this by building the project:

```bash
dotnet build
```

If you have zero build issues: congratulations! We're ready to move onto our next step: defining the Steps that process this data.

You've defined schemas and registered raw datasets in the catalog. Next, you'll create a Flow that processes this data into a format ready for data science!

**Continue to: [Create a Flow](03-create-a-pipeline.md)**
