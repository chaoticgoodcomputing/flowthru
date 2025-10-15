### Service Registration

[File Source](/docs/external/nsync/repo/NSync/src/NSync.Server.Jobs.Internal/Extensions/ServiceExtensions.cs)

```cs
using ChainSharp.Effect.Data.Extensions;
using ChainSharp.Effect.Data.Postgres.Extensions;
using ChainSharp.Effect.Extensions;
using ChainSharp.Effect.Json.Extensions;
using ChainSharp.Effect.Mediator.Extensions;
using ChainSharp.Effect.Parameter.Extensions;

# ...

public static IServiceCollection AddChainSharpServices(
        this IServiceCollection services,
        NSyncConfiguration configuration
    ) =>
        services.AddChainSharpEffects(
            options =>
                options
                    .AddEffectWorkflowBus(
                        assemblies:
                        [
                            typeof(AssemblyMarker).Assembly,
                            typeof(AssemblyMarker).Assembly,
                            typeof(Workflows.AssemblyMarker).Assembly,
                            typeof(Server.Handlers.AssemblyMarker).Assembly
                        ]
                    )
                    .AddJsonEffect()
        );
```

### Calling Workflows

[File source](/docs/external/nsync/repo/NSync/src/NSync.Server.Jobs.Internal/Handlers/RunWorkflowBusWithGarbageCollection.cs)

```cs
public class RunWorkflowBus(IWorkflowBus workflowBus)
{
    private async Task ProcessTaskRunner<TInput, TOutput>(TInput input)
        => await workflowBus.RunAsync<TOutput>(input);
}
```


### Workflow Example

```cs
using ChainSharp.Effect.Services.EffectWorkflow;
using LanguageExt;
using NetSuite.DataAccess.Raw;
using NSync.Workflows.NewTypes;
using NSync.Workflows.Workflows.TransformNetSuiteDataWorkflow.Steps.ClearSilverData;
using NSync.Workflows.Workflows.TransformNetSuiteDataWorkflow.Steps.FetchBronzeTableContent;
using NSync.Workflows.Workflows.TransformNetSuiteDataWorkflow.Steps.ImportSilverTableData;

# ...

public class TransformNetSuiteDataWorkflow
    : EffectWorkflow<SilverRequest, Unit>,
        ITransformNetSuiteDataWorkflow
{
    protected override async Task<Either<Exception, Unit>> RunInternal(SilverRequest input) =>
        Activate(input)
            .Chain<ClearSilverData>()
            .Chain<ImportSilverTableData>()
            .Resolve();
}
```

### Step Example

```cs
using ChainSharp.Step;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NetSuite.DataAccess.Raw;
using NetSuite.DataAccess.Typed;
using NSync.Services.NetsuiteTableBatchRetriever;
using NSync.Services.TruncateTableService;
using NSync.Workflows.NewTypes;

# ...

public class ClearSilverData(
    IBronzeDataContext dataContext,
    ITruncateTableService truncateTableService,
    ILogger<ClearSilverData> logger
) : Step<SilverRequest, Unit>
{
    public override async Task<Unit> Run(SilverRequest input)
    {
        var schema = input.Table.GetAttributeValue<TypedSchemaNameAttribute, string>();
        var tableName = input.Table.GetAttributeValue<TypedTableNameAttribute, string>();

        logger.LogInformation($"Starting ClearSilverData step for {schema}.{tableName}");

        await truncateTableService.TruncateTableAsync(dataContext.Raw, schema, tableName);

        logger.LogInformation(
            $"ClearSilverData completed: successfully truncated {schema}.{tableName}"
        );

        return Unit.Default;
    }
}
```
