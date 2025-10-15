# ChainSharp External Source Notes

ChainSharp is a .NET library for Railway Oriented Programming, providing a structured approach to implementing business logic with clear error handling and workflow organization.

## Key Takeaways

1. **Railway Oriented Programming Pattern**: ChainSharp implements the Railway Oriented Programming pattern, which provides a structured way to handle success and failure paths in a workflow.
2. **Workflow and Step Architecture**: The library is built around the concepts of Workflows and Steps, where Workflows chain together discrete Steps to form a complete business operation.
3. **Dependency Injection Integration**: ChainSharp integrates with .NET's dependency injection system, making it easy to register and resolve workflows and their dependencies.
4. **Testing Support**: The library provides a testing framework that makes it easy to test workflows and steps in isolation.
5. **Error Handling**: ChainSharp provides built-in error handling through the Either<Exception, T> type, which represents either a success (Right) or failure (Left) path.

## Applications of Takeaways

### Railway Oriented Programming Pattern

ChainSharp's implementation of Railway Oriented Programming provides a clear and consistent way to handle success and failure paths in business logic. This approach helps to:

- Separate happy path logic from error handling
- Make error handling explicit and consistent
- Reduce nested try/catch blocks
- Improve code readability and maintainability

#### Connections

This pattern is similar to the Result pattern used in functional programming languages like F# and Haskell. It's also related to the Either monad in functional programming.

#### Applications

In the NIRA application, we can use ChainSharp's Railway Oriented Programming to implement business workflows with clear error handling. For example, when creating a transaction code, we can define a workflow that validates the input, saves the transaction code to the database, and returns the result, with explicit error handling at each step.

### Workflow and Step Architecture

ChainSharp's Workflow and Step architecture provides a structured way to organize business logic:

- **Workflows**: Define a complete business operation by chaining together discrete steps
- **Steps**: Perform a specific operation and can be tested independently
- **Chaining**: Connect steps in sequence, with each step's output becoming the next step's input

#### Connections

This architecture is similar to the Chain of Responsibility pattern, but with a focus on transforming data through a sequence of operations rather than just handling requests.

#### Applications

In the NIRA application, we can use ChainSharp's Workflow and Step architecture to implement business workflows as a series of discrete steps. For example:

```csharp
public class CreateTransactionCodeWorkflow : EffectWorkflow<CreateTransactionCodeInput, TransactionCodeDto>, ICreateTransactionCodeWorkflow
{
    protected override async Task<Either<Exception, TransactionCodeDto>> RunInternal(CreateTransactionCodeInput input) =>
        Activate(input)
            .Chain<ValidateCreateTransactionCodeStep>()
            .Chain<SaveTransactionCodeStep>()
            .Resolve();
}
```

### Dependency Injection Integration

ChainSharp integrates with .NET's dependency injection system, making it easy to register and resolve workflows and their dependencies:

- **Service Registration**: Register ChainSharp services with the dependency injection container
- **Workflow Bus**: Use the WorkflowBus to invoke workflows and resolve their dependencies
- **Assembly Scanning**: Automatically discover and register workflows and steps from specified assemblies

#### Connections

This integration is similar to how other .NET libraries like MediatR integrate with dependency injection.

#### Applications

In the NIRA application, we can use ChainSharp's dependency injection integration to register workflows and their dependencies:

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddBusinessServices(this IServiceCollection services)
{
    // Register ChainSharp services
    services.AddChainSharpEffects(
        options => options
            .AddEffectWorkflowBus(
                assemblies: [
                    typeof(AssemblyMarker).Assembly
                ]
            )
    );
    
    return services;
}
```

### Testing Support

ChainSharp provides a testing framework that makes it easy to test workflows and steps in isolation:

- **Workflow Testing**: Test the end-to-end behavior of a workflow
- **Step Testing**: Test the behavior of individual steps
- **Mock Integration**: Easily mock dependencies for testing

#### Connections

This testing approach is similar to how other testing frameworks like xUnit and NUnit support testing with mocks and dependency injection.

#### Applications

In the NIRA application, we can use ChainSharp's testing support to test workflows and steps:

```csharp
// Workflow test
[Test]
public async Task RunAsync_ValidInput_ReturnsTransactionCode()
{
    // Arrange
    var input = new CreateTransactionCodeInput { /* ... */ };
    
    // Act
    var result = await WorkflowBus.RunAsync<TransactionCodeDto>(input);
    
    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Code, Is.EqualTo("003"));
}

// Step test
[Test]
public async Task Run_ValidInput_ReturnsInput()
{
    // Arrange
    var input = new CreateTransactionCodeInput { /* ... */ };
    
    // Act
    var result = await _step.Run(input);
    
    // Assert
    Assert.That(result, Is.SameAs(input));
}
```

### Error Handling

ChainSharp provides built-in error handling through the Either<Exception, T> type, which represents either a success (Right) or failure (Left) path:

- **Either Type**: Represents either a success (Right) or failure (Left) path
- **Exception Handling**: Automatically catches exceptions thrown by steps and converts them to Left values
- **Error Propagation**: Errors are propagated through the workflow chain

#### Connections

This error handling approach is similar to how functional programming languages like F# and Haskell handle errors with the Either monad.

#### Applications

In the NIRA application, we can use ChainSharp's error handling to handle errors in workflows:

```csharp
// In a controller or service
public async Task<IActionResult> CreateTransactionCode(CreateTransactionCodeRequest request)
{
    try
    {
        var input = new CreateTransactionCodeInput { /* ... */ };
        var result = await _workflowBus.RunAsync<TransactionCodeDto>(input);
        return Ok(result);
    }
    catch (ValidationException ex)
    {
        return BadRequest(ex.Errors);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}
```

## Required Boilerplate

### Workflow Boilerplate

```csharp
// Interface
public interface ICreateEntityWorkflow : IEffectWorkflow<CreateEntityInput, EntityOutput> { }

// Implementation
public class CreateEntityWorkflow : EffectWorkflow<CreateEntityInput, EntityOutput>, ICreateEntityWorkflow
{
    protected override async Task<Either<Exception, EntityOutput>> RunInternal(CreateEntityInput input) =>
        Activate(input)
            .Chain<ValidateEntity>()
            .Chain<SaveEntity>()
            .Resolve();
}
```

### Step Boilerplate

```csharp
public class ValidateEntity : Step<CreateEntityInput, ValidatedEntity>
{
    private readonly IEntityValidator _validator;

    public ValidateEntity(IEntityValidator validator)
    {
        _validator = validator;
    }

    public override async Task<ValidatedEntity> Run(CreateEntityInput input)
    {
        var validationResult = await _validator.ValidateAsync(input);
        
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
            
        return new ValidatedEntity
        {
            Id = input.Id,
            Name = input.Name,
            // Other properties
        };
    }
}
```

### Test Boilerplate

```csharp
// Test base class
public abstract class BusinessTestBase
{
    protected ServiceProvider ServiceProvider { get; private set; }
    protected IWorkflowBus WorkflowBus { get; private set; }
    
    [SetUp]
    public virtual void Setup()
    {
        // Create a new service collection
        var services = new ServiceCollection();
        
        // Register ChainSharp services
        services.AddChainSharpEffects(
            options => options
                .AddEffectWorkflowBus(
                    assemblies: [
                        typeof(NIRA.Business.AssemblyMarker).Assembly,
                        typeof(BusinessTestBase).Assembly
                    ]
                )
        );
        
        // Register mocks and dependencies
        RegisterMocks(services);
        
        // Register workflows and steps
        RegisterWorkflowsAndSteps(services);
        
        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();
        
        // Get the workflow bus
        WorkflowBus = ServiceProvider.GetRequiredService<IWorkflowBus>();
    }
    
    // Override these methods in derived classes
    protected virtual void RegisterMocks(IServiceCollection services) { }
    protected virtual void RegisterWorkflowsAndSteps(IServiceCollection services) { }
    
    [TearDown]
    public virtual void TearDown()
    {
        ServiceProvider?.Dispose();
    }
}

// Workflow test
[TestFixture]
public class CreateEntityWorkflowTests : BusinessTestBase
{
    [Test]
    public async Task RunAsync_ValidInput_ReturnsEntityOutput()
    {
        // Arrange
        var input = new CreateEntityInput { /* ... */ };
        
        // Act
        var result = await WorkflowBus.RunAsync<EntityOutput>(input);
        
        // Assert
        Assert.That(result, Is.Not.Null);
    }
}

// Step test
[TestFixture]
public class ValidateEntityStepTests
{
    private ValidateEntityStep _step;
    
    [SetUp]
    public void Setup()
    {
        _step = new ValidateEntityStep(/* ... */);
    }
    
    [Test]
    public async Task Run_ValidInput_ReturnsValidatedEntity()
    {
        // Arrange
        var input = new CreateEntityInput { /* ... */ };
        
        // Act
        var result = await _step.Run(input);
        
        // Assert
        Assert.That(result, Is.Not.Null);
    }
}
