using Flowthru.Nodes;
using Flowthru.Tests.Fixtures.TestCatalogs;

namespace Flowthru.Tests.Fixtures.TestNodes;

/// <summary>
/// Simple passthrough node that copies input to output unchanged.
/// </summary>
public class PassthroughNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams>
{
    protected override Task<IEnumerable<TestData>> Transform(IEnumerable<TestData> input)
    {
        return Task.FromResult(input);
    }
}

/// <summary>
/// Node that always throws an exception during execution.
/// </summary>
public class FailingNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams>
{
    public string ErrorMessage { get; set; } = "Test node failure";
    
    protected override Task<IEnumerable<TestData>> Transform(IEnumerable<TestData> input)
    {
        throw new InvalidOperationException(ErrorMessage);
    }
}

/// <summary>
/// Node that introduces a delay during execution.
/// </summary>
public class DelayedNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams>
{
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(100);
    
    protected override async Task<IEnumerable<TestData>> Transform(IEnumerable<TestData> input)
    {
        await Task.Delay(Delay);
        return input;
    }
}

/// <summary>
/// Node that transforms data by incrementing the Id field.
/// </summary>
public class IncrementNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams>
{
    protected override Task<IEnumerable<TestData>> Transform(IEnumerable<TestData> input)
    {
        return Task.FromResult(input.Select(item => item with { Id = item.Id + 1 }));
    }
}

/// <summary>
/// Node that transforms data by doubling the Value field.
/// </summary>
public class DoubleValueNode : NodeBase<IEnumerable<TestData>, IEnumerable<TestData>, NoParams>
{
    protected override Task<IEnumerable<TestData>> Transform(IEnumerable<TestData> input)
    {
        return Task.FromResult(input.Select(item => item with { Value = item.Value * 2 }));
    }
}

/// <summary>
/// Node that merges two datasets into one.
/// </summary>
public class MergeNode : NodeBase<(IEnumerable<TestData>, IEnumerable<TestData>), IEnumerable<TestData>, NoParams>
{
    protected override Task<IEnumerable<TestData>> Transform((IEnumerable<TestData>, IEnumerable<TestData>) input)
    {
        return Task.FromResult(input.Item1.Concat(input.Item2));
    }
}

/// <summary>
/// Node that splits a dataset into two halves.
/// </summary>
public class SplitNode : NodeBase<IEnumerable<TestData>, (IEnumerable<TestData>, IEnumerable<TestData>), NoParams>
{
    protected override Task<(IEnumerable<TestData>, IEnumerable<TestData>)> Transform(IEnumerable<TestData> input)
    {
        var list = input.ToList();
        var midpoint = list.Count / 2;
        return Task.FromResult((list.Take(midpoint).AsEnumerable(), list.Skip(midpoint).AsEnumerable()));
    }
}
