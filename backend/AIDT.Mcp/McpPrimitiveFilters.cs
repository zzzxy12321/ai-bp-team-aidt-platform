using AIDT.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace AIDT.Mcp;

public static class McpPrimitiveFilters
{
    public static IMcpServerBuilder WithAidtPrimitiveFilters(this IMcpServerBuilder builder)
    {
        return builder.WithRequestFilters(filters =>
        {
            filters.AddListToolsFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                var callContext = await context.Services!
                    .GetRequiredService<IMcpCallContextAccessor>()
                    .GetRequiredAsync(cancellationToken);
                var catalog = context.Services!.GetRequiredService<IMcpToolCatalog>();
                var visibleNames = catalog.List()
                    .Where(tool =>
                        callContext.Identity.Scopes.Contains(tool.Scope, StringComparer.Ordinal)
                        && IsAgentCapabilityAllowed(
                            callContext.Identity,
                            tool.AgentCapability))
                    .Select(tool => tool.Name)
                    .ToHashSet(StringComparer.Ordinal);
                result.Tools = result.Tools
                    .Where(tool => visibleNames.Contains(tool.Name))
                    .ToList();
                return result;
            });
            filters.AddListResourcesFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                var callContext = await context.Services!
                    .GetRequiredService<IMcpCallContextAccessor>()
                    .GetRequiredAsync(cancellationToken);
                result.Resources = result.Resources
                    .Where(resource =>
                        resource.Uri.StartsWith("aidt://navigation", StringComparison.Ordinal)
                        && callContext.Identity.Scopes.Contains(
                            AIDT.Application.Contracts.McpScopeNames.Read,
                            StringComparer.Ordinal)
                        && IsAgentCapabilityAllowed(callContext.Identity, "identity.read"))
                    .ToList();
                return result;
            });
            filters.AddListResourceTemplatesFilter(next => async (context, cancellationToken) =>
            {
                var result = await next(context, cancellationToken);
                var callContext = await context.Services!
                    .GetRequiredService<IMcpCallContextAccessor>()
                    .GetRequiredAsync(cancellationToken);
                result.ResourceTemplates = result.ResourceTemplates
                    .Where(resource =>
                        (resource.UriTemplate.StartsWith("aidt://files/", StringComparison.Ordinal)
                            && callContext.Identity.Scopes.Contains(
                                AIDT.Application.Contracts.McpScopeNames.Files,
                                StringComparer.Ordinal)
                            && IsAgentCapabilityAllowed(
                                callContext.Identity,
                                "file.metadata.read"))
                        || (!resource.UriTemplate.StartsWith("aidt://files/", StringComparison.Ordinal)
                            && callContext.Identity.Scopes.Contains(
                                AIDT.Application.Contracts.McpScopeNames.Read,
                                StringComparer.Ordinal)
                            && IsAgentCapabilityAllowed(
                                callContext.Identity,
                                CapabilityForResource(resource.UriTemplate))))
                    .ToList();
                return result;
            });
        });
    }

    private static bool IsAgentCapabilityAllowed(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        string capability) =>
        !identity.AgentProfileId.HasValue
        || (identity.AllowedCapabilities ?? [])
            .Contains(capability, StringComparer.OrdinalIgnoreCase);

    private static string CapabilityForResource(string uriTemplate) =>
        uriTemplate switch
        {
            var value when value.StartsWith("aidt://events/", StringComparison.Ordinal) =>
                "event.inbox.review",
            var value when value.StartsWith("aidt://project-tasks/", StringComparison.Ordinal) =>
                "project.read",
            var value when value.StartsWith("aidt://projects/", StringComparison.Ordinal) =>
                "project.read",
            var value when value.StartsWith("aidt://workflow-instances/", StringComparison.Ordinal) =>
                "workflow.read",
            _ => "identity.read"
        };
}
