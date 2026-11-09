using Elsa.Common.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;

namespace AIDT.Workflow;

public sealed class ElsaTransitionRecorder(
    IWorkflowDefinitionStorePopulator definitionStorePopulator,
    IWorkflowRuntime workflowRuntime) : IDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public async Task<string> RecordAsync(
        Guid workflowInstanceId,
        string nodeCode,
        string decision,
        CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);
        var workflowClient = await workflowRuntime.CreateClientAsync(cancellationToken);
        await workflowClient.CreateInstanceAsync(
            new CreateWorkflowInstanceRequest
            {
                Name = $"AIDT:{workflowInstanceId:D}:{nodeCode}:{decision}",
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                    AidtTransitionWorkflow.DefinitionId,
                    VersionOptions.Published),
                Input = new Dictionary<string, object>
                {
                    ["Marker"] = $"{workflowInstanceId:D}|{nodeCode}|{decision}"
                }
            },
            cancellationToken);
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty, cancellationToken);
        var state = await workflowClient.ExportStateAsync(cancellationToken);
        return state.Id;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await definitionStorePopulator.PopulateStoreAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
