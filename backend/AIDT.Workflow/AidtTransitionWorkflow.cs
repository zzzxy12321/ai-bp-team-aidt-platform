using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace AIDT.Workflow;

public sealed class AidtTransitionWorkflow : WorkflowBase
{
    public const string DefinitionId = nameof(AidtTransitionWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        var marker = builder.WithInput<string>("Marker", "AIDT business transition marker.");
        builder.Root = new Sequence
        {
            Activities =
            {
                new AidtTransitionActivity
                {
                    Marker = new Input<string>(context => context.GetInput<string>(marker) ?? "unknown")
                }
            }
        };
    }
}

public sealed class AidtTransitionActivity : CodeActivity
{
    public Input<string> Marker { get; set; } = default!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        _ = Marker.Get(context);
        return ValueTask.CompletedTask;
    }
}
