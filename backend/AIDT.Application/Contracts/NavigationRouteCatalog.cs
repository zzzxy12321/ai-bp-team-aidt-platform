using AIDT.Domain.Identity;

namespace AIDT.Application.Contracts;

public sealed record RegisteredNavigationRoute(
    string Route,
    string? RequiredResource,
    AccessAction? RequiredAction,
    string ItemType);

public static class NavigationRouteCatalog
{
    private static readonly Dictionary<string, RegisteredNavigationRoute> Routes =
        new Dictionary<string, RegisteredNavigationRoute>(StringComparer.OrdinalIgnoreCase)
        {
            ["/"] = new("/", "dashboard", AccessAction.Read, "System"),
            ["/confirmations"] = new("/confirmations", null, null, "Inbox"),
            ["/scenarios"] = new("/scenarios", "scenario", AccessAction.Read, "Scenario"),
            ["/department"] = new("/department", "department", AccessAction.Read, "Workspace"),
            ["/documents"] = new("/documents", "file", AccessAction.Read, "File"),
            ["/minutes"] = new("/minutes", "minutes", AccessAction.Read, "Minutes"),
            ["/projects"] = new("/projects", "project", AccessAction.Read, "Project"),
            ["/inbox"] = new("/inbox", "event-inbox", AccessAction.Read, "Inbox"),
            ["/management"] = new("/management", "identity-admin", AccessAction.Manage, "Administration"),
            ["/workflows"] = new("/workflows", "workflow-task", AccessAction.Read, "Workflow"),
            ["/objects"] = new("/objects", "business-object", AccessAction.Read, "BusinessObject"),
            ["/modeling"] = new("/modeling", "object-model", AccessAction.Read, "Administration"),
            ["/audit"] = new("/audit", "audit", AccessAction.Read, "Administration"),
            ["/agents"] = new("/agents", "agent", AccessAction.Read, "Agent"),
            ["/workflow-policies"] = new("/workflow-policies", "workflow-policy", AccessAction.Read, "Administration"),
            ["/configurations"] = new("/configurations", "configuration", AccessAction.Read, "Administration"),
            ["/iam"] = new("/iam", "identity-admin", AccessAction.Read, "Administration"),
            ["/mcp-center"] = new("/mcp-center", "mcp", AccessAction.Read, "Administration"),
            ["/mcp-confirmations"] = new("/mcp-confirmations", null, null, "Inbox")
        };

    public static bool TryValidate(
        string? route,
        string? resource,
        string? action,
        out string normalizedRoute)
    {
        normalizedRoute = string.Empty;
        if (string.IsNullOrWhiteSpace(route))
        {
            return false;
        }

        var raw = route.Trim();
        if (!raw.StartsWith('/') || raw.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        var separator = raw.IndexOf('?');
        var path = separator < 0 ? raw : raw[..separator];
        var query = separator < 0 ? string.Empty : raw[(separator + 1)..];
        if (!Routes.TryGetValue(path, out var registration))
        {
            return false;
        }

        if (!IsAllowedDepartmentQuery(query))
        {
            return false;
        }

        if (!string.Equals(resource?.Trim(), registration.RequiredResource, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (registration.RequiredAction.HasValue)
        {
            if (!Enum.TryParse<AccessAction>(action, true, out var parsed)
                || parsed != registration.RequiredAction.Value)
            {
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        normalizedRoute = raw;
        return true;
    }

    private static bool IsAllowedDepartmentQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return pairs.Length == 1
            && pairs[0].StartsWith("departmentId=", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(pairs[0]["departmentId=".Length..], out _);
    }
}
