using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AIDT.UnitTests.Contracts;

public sealed class MigrationModelTests
{
    [Fact]
    public void ModelAuthorsExpectedCoreTablesWithoutOpeningConnection()
    {
        var options = new DbContextOptionsBuilder<AidtDbContext>()
            .UseSqlServer(
                "Server=127.0.0.1;Database=AIDT_Schema_Only;Integrated Security=True;Connect Timeout=1")
            .Options;
        using var context = new AidtDbContext(options);

        var tableNames = context.Model.GetEntityTypes()
            .Select(entity => $"{entity.GetSchema()}.{entity.GetTableName()}")
            .Where(tableName => !tableName.EndsWith("__EFMigrationsHistory", StringComparison.Ordinal))
            .ToArray();

        tableNames.Length.ShouldBe(71);
        tableNames.ShouldContain("Identity.Department");
        tableNames.ShouldContain("Identity.UserDepartmentMembership");
        tableNames.ShouldContain("Document.AttachmentLink");
        tableNames.ShouldContain("Minutes.MinutesSource");
        tableNames.ShouldContain("Minutes.MinutesActionItem");
        tableNames.ShouldContain("Minutes.MinutesTask");
        tableNames.ShouldContain("Workflow.WorkflowNodePolicy");
        tableNames.ShouldContain("Workflow.WorkflowNotification");
        tableNames.ShouldContain("Agent.AgentGatewayCall");
        tableNames.ShouldContain("BusinessObject.BusinessObject");
        tableNames.ShouldContain("Workflow.WorkflowTask");
        tableNames.ShouldContain("Identity.UserRoleAssignment");
        tableNames.ShouldContain("Identity.AccessChangeRequest");
        tableNames.ShouldContain("Navigation.NavigationWorkspace");
        tableNames.ShouldContain("Navigation.NavigationVersion");
        tableNames.ShouldContain("Navigation.NavigationItem");
        tableNames.ShouldContain("Navigation.NavigationUserPreference");
        tableNames.ShouldContain("Workspace.WorkbenchTemplate");
        tableNames.ShouldContain("Workspace.WorkbenchTemplateVersion");
        tableNames.ShouldContain("Workspace.WorkbenchCard");
        tableNames.ShouldContain("Workspace.WorkbenchAssignment");
        tableNames.ShouldContain("Audit.AuditEvent");
        tableNames.ShouldContain("Integration.OutboxEvent");
        tableNames.ShouldContain("Integration.IdempotencyRecord");
        tableNames.ShouldContain("Agent.AgentRun");
        tableNames.ShouldContain("Agent.AgentProfileVersion");
        tableNames.ShouldContain("PMO.ProjectProfile");
        tableNames.ShouldContain("PMO.ProjectMember");
        tableNames.ShouldContain("PMO.ProjectMilestone");
        tableNames.ShouldContain("PMO.ProjectRelation");
        tableNames.ShouldContain("PMO.ProjectTask");
        tableNames.ShouldContain("PMO.EventInbox");
        tableNames.ShouldContain("PMO.EventSuggestion");
        tableNames.ShouldContain("Mcp.Client");
        tableNames.ShouldContain("Mcp.AuthorizationRequest");
        tableNames.ShouldContain("Mcp.AuthorizationCode");
        tableNames.ShouldContain("Mcp.AccessToken");
        tableNames.ShouldContain("Mcp.HumanConfirmation");
        tableNames.ShouldContain("Metadata.ObjectTypeVersion");
        tableNames.ShouldContain("Scenario.ScenarioDefinition");
        tableNames.ShouldContain("Scenario.ScenarioVersion");
        tableNames.ShouldContain("Scenario.ScenarioLaunch");

        var businessObject = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.BusinessObjectEntity));
        businessObject.ShouldNotBeNull();
        businessObject!.FindProperty("DepartmentId").ShouldNotBeNull();
        businessObject.FindProperty("EnvironmentCode")!.GetMaxLength().ShouldBe(16);
        businessObject.FindProperty("Title").ShouldNotBeNull();
        businessObject.FindProperty("CreatedByUserId").ShouldNotBeNull();

        var objectTypeVersion = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.ObjectTypeVersionEntity));
        objectTypeVersion.ShouldNotBeNull();
        objectTypeVersion!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == "ObjectTypeId")
            && index.Properties.Any(property => property.Name == "Version"));

        var fieldDefinition = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.FieldDefinitionEntity));
        fieldDefinition.ShouldNotBeNull();
        fieldDefinition!.FindProperty("DefaultValue").ShouldNotBeNull();
        fieldDefinition.FindProperty("OptionsJson").ShouldNotBeNull();
        fieldDefinition.FindProperty("ShowInList").ShouldNotBeNull();
        fieldDefinition.FindProperty("Filterable").ShouldNotBeNull();
        fieldDefinition.FindProperty("SortOrder").ShouldNotBeNull();

        var membership = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.UserDepartmentMembershipEntity));
        membership.ShouldNotBeNull();
        membership!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.GetFilter()!.Contains("IsPrimary", StringComparison.Ordinal)
            && index.Properties.Any(property => property.Name == "UserId"));

        var designTimeModel = context
            .GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>()
            .Model;
        var attachmentLink = designTimeModel.GetEntityTypes().Single(entity =>
            entity.ClrType == typeof(AIDT.Infrastructure.Persistence.AttachmentLinkEntity));
        attachmentLink.ShouldNotBeNull();
        attachmentLink!.GetCheckConstraints().ShouldContain(constraint =>
            constraint.Name == "CK_AttachmentLink_EntityType");

        var task = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.WorkflowTaskEntity));
        task.ShouldNotBeNull();
        task!.FindProperty("NodePolicyId").ShouldNotBeNull();
        task.FindProperty("DecisionMode").ShouldNotBeNull();

        var gatewayCall = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.AgentGatewayCallEntity));
        gatewayCall.ShouldNotBeNull();
        gatewayCall!.GetIndexes().ShouldContain(index =>
            index.Properties.Any(property => property.Name == "AgentRunId"));

        var scenarioVersion = designTimeModel.GetEntityTypes().Single(entity =>
            entity.ClrType == typeof(AIDT.Infrastructure.Persistence.ScenarioVersionEntity));
        scenarioVersion.ShouldNotBeNull();
        scenarioVersion!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == "ScenarioId")
            && index.Properties.Any(property => property.Name == "VersionNumber"));
        scenarioVersion.GetCheckConstraints().ShouldContain(constraint =>
            constraint.Sql.Contains("ISJSON([PayloadJson])", StringComparison.Ordinal));

        var scenarioLaunch = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.ScenarioLaunchEntity));
        scenarioLaunch.ShouldNotBeNull();
        scenarioLaunch!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == "OrganizationId")
            && index.Properties.Any(property => property.Name == "StartedBy")
            && index.Properties.Any(property => property.Name == "IdempotencyKey"));

        var agentVersion = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.AgentProfileVersionEntity));
        agentVersion.ShouldNotBeNull();
        agentVersion!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == "AgentProfileId")
            && index.GetFilter() == "[Status] = 'Published'");
        agentVersion.FindProperty("DataScopesJson").ShouldNotBeNull();
        agentVersion.FindProperty("RequiredConfirmationsJson").ShouldNotBeNull();

        var agentProfile = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.AgentProfileEntity));
        agentProfile.ShouldNotBeNull();
        agentProfile!.GetIndexes().ShouldContain(index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == "Code"));
        agentProfile.FindProperty("CurrentPublishedVersionId").ShouldNotBeNull();

        var minutesAction = context.Model.FindEntityType(
            typeof(AIDT.Infrastructure.Persistence.MinutesActionItemEntity));
        minutesAction.ShouldNotBeNull();
        minutesAction!.FindProperty("ProjectId").ShouldNotBeNull();
        minutesAction.FindProperty("BusinessObjectId").ShouldNotBeNull();
        minutesAction.FindProperty("WorkflowInstanceId").ShouldNotBeNull();
        context.Model.FindEntityType(typeof(AIDT.Infrastructure.Persistence.MinutesTaskEntity))!
            .FindProperty("ProjectId")
            .ShouldNotBeNull();
    }
}
