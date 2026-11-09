using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIDT.Api.Contracts;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;

namespace AIDT.UnitTests.Api;

public sealed class IamNavigationWorkflowRoleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] CommonNavigationItemKeys =
        ["MY_WORK", "CONFIRMATIONS", "TODO", "EVENT_INBOX"];

    private static readonly JsonSerializerOptions ClientJsonOptions = CreateJsonOptions();
    private readonly WebApplicationFactory<Program> _factory;

    public IamNavigationWorkflowRoleApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [SqlIntegrationFact]
    public async Task Navigation_PublishesDepartmentWorkspaceAndRespectsMenuPermissions()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var teamCode = $"TEAM-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        var team = await PostJsonAsync<DepartmentTreeNodeDto>(
            admin,
            "/api/v1/iam/departments",
            new CreateIamDepartmentRequest(
                teamCode,
                "研发验收班组",
                rnd.Id,
                "Team",
                null,
                200),
            null);
        team.ParentId.ShouldBe(rnd.Id);
        team.HierarchyType.ShouldBe("Team");
        var deactivatedTeam = await PostJsonAsync<DepartmentTreeNodeDto>(
            admin,
            $"/api/v1/iam/departments/{team.Id}/actions/deactivate",
            new { },
            null);
        deactivatedTeam.IsActive.ShouldBeFalse();
        var blockedDepartment = await PostJsonRawAsync(
            admin,
            $"/api/v1/iam/departments/{rnd.Id}/actions/deactivate",
            new { },
            null);
        blockedDepartment.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var workspaceKey = $"NAV-E2E-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var created = await PostJsonAsync<NavigationWorkspaceDto>(
            admin,
            "/api/v1/navigation/workspaces",
            new CreateNavigationWorkspaceRequest(
                workspaceKey,
                "研发部导航验收",
                "DepartmentWorkspace",
                Guid.Parse(rnd.Id),
                "DepartmentWorkspace",
                ["Projects", "Minutes"]),
            $"nav-create-{Guid.NewGuid():N}");
        created.Items.ShouldNotContain(item => item.ItemKey == "MY_WORK");
        created.Items.ShouldNotContain(item => item.ItemKey == "TODO");
        created.Items.ShouldNotContain(item => item.ItemKey == "EVENT_INBOX");
        created.Items.ShouldContain(item => item.ItemKey == "DEPARTMENT_HOME");

        var published = await PostJsonAsync<NavigationWorkspaceDto>(
            admin,
            $"/api/v1/navigation/workspaces/{created.Id}/actions/publish",
            new { },
            $"nav-publish-{Guid.NewGuid():N}");
        published.Status.ShouldBe("Published");
        published.IsTestData.ShouldBeTrue();
        published.IsDefault.ShouldBeFalse();
        var adminCurrent = await GetAsync<CurrentNavigationDto>(
            admin.Client,
            "/api/v1/navigation/current");
        adminCurrent.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);
        adminCurrent.Groups
            .Single(group => group.DepartmentCode == "RND")
            .WorkspaceKey.ShouldBe("DEPT-RND");

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var current = await GetAsync<CurrentNavigationDto>(researcher.Client, "/api/v1/navigation/current");
        current.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);
        current.Groups.Count(group => group.Layer == "Company").ShouldBe(1);
        var company = current.Groups.Single(group => group.Layer == "Company");
        company.Items.Select(item => item.ItemKey)
            .ShouldBe(
                CommonNavigationItemKeys,
                ignoreOrder: true);
        current.Groups
            .Where(group => group.Layer == "Department")
            .SelectMany(group => group.Items)
            .ShouldNotContain(item =>
                CommonNavigationItemKeys.Contains(item.ItemKey));

        var retired = await PostJsonAsync<NavigationWorkspaceDto>(
            admin,
            $"/api/v1/navigation/workspaces/{created.Id}/actions/retire",
            new { },
            null);
        retired.Status.ShouldBe("Retired");
        var adminHidden = await GetAsync<CurrentNavigationDto>(
            admin.Client,
            "/api/v1/navigation/current");
        adminHidden.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);
        var hidden = await GetAsync<CurrentNavigationDto>(researcher.Client, "/api/v1/navigation/current");
        hidden.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);

        await PostJsonAsync<NavigationWorkspaceDto>(
            admin,
            $"/api/v1/navigation/workspaces/{created.Id}/actions/restore",
            new { },
            null);
        var restored = await GetAsync<CurrentNavigationDto>(
            admin.Client,
            "/api/v1/navigation/current");
        restored.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);
        var researcherRestored = await GetAsync<CurrentNavigationDto>(
            researcher.Client,
            "/api/v1/navigation/current");
        researcherRestored.Groups.ShouldNotContain(group => group.WorkspaceKey == workspaceKey);
        await PostJsonAsync<NavigationWorkspaceDto>(
            admin,
            $"/api/v1/navigation/workspaces/{created.Id}/actions/retire",
            new { },
            null);

        var forbidden = await PostJsonRawAsync(
            researcher,
            "/api/v1/navigation/workspaces",
            new CreateNavigationWorkspaceRequest(
                $"NO-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                "无权限工作区",
                "DepartmentWorkspace",
                Guid.Parse(rnd.Id),
                "DepartmentWorkspace",
                []),
            $"nav-deny-{Guid.NewGuid():N}");
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [SqlIntegrationFact]
    public async Task ProjectManager_ApprovesFrozenManagerRoleWithoutPlatformAdmin()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var roleRows = await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles");
        var configuredManagerRole = roleRows.Single(item => item.Code == "business-manager-reviewer");
        var configuredQualityRole = roleRows.Single(item => item.Code == "business-quality-reviewer");
        var configuredPmId = await GetUserIdAsync("demo.pm");
        var securityId = await GetUserIdAsync("demo.security");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{configuredManagerRole.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(configuredPmId),
                        "department",
                        rnd.Id,
                        true),
                    new SetRoleMemberItem(
                        Guid.Parse(securityId),
                        "department",
                        rnd.Id,
                        true)
                ],
                configuredManagerRole.Version),
            $"role-baseline-{Guid.NewGuid():N}");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{configuredQualityRole.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(await GetUserIdAsync("demo.qa")),
                        "organization",
                        "organization",
                        true)
                ],
                configuredQualityRole.Version),
            $"role-quality-baseline-{Guid.NewGuid():N}");

        var createdProject = await PostJsonAsync<ProjectDto>(
            admin,
            "/api/v1/projects",
            new CreateProjectRequest(
                $"PM-ROLE-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                "项目经理真实审批验收",
                Guid.Parse(await GetUserIdAsync("demo.pm")),
                Guid.Parse(rnd.Id),
                null,
                null,
                "验证业务角色节点冻结与真实项目经理审批",
                "验收",
                "Internal"),
            $"pm-object-{Guid.NewGuid():N}");

        var configurationKey = $"WFROLE-E2E-{DateTimeOffset.UtcNow:HHmmssfff}";
        var configuration = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            "/api/v1/configurations",
            new CreateWorkflowConfigurationRequest(
                configurationKey,
                "项目经理角色审批验收",
                "研发发起、经理角色审核、质量角色复核",
                Guid.Parse(rnd.Id),
                "PROJECT",
                "RD_INITIATE",
                [
                    new WorkflowConfigurationNodeRequest(
                        "RD_INITIATE",
                        "研发发起",
                        "demo.researcher",
                        "MANAGER_REVIEW",
                        false,
                        null,
                        "SpecificUser",
                        null,
                        null),
                    new WorkflowConfigurationNodeRequest(
                        "MANAGER_REVIEW",
                        "经理审核",
                        null,
                        "QA_REVIEW",
                        false,
                        null,
                        "BusinessRole",
                        "business-manager-reviewer",
                        null),
                    new WorkflowConfigurationNodeRequest(
                        "QA_REVIEW",
                        "质量复核",
                        null,
                        null,
                        true,
                        null,
                        "BusinessRole",
                        "business-quality-reviewer",
                        null)
                ]),
            $"wfrole-config-{Guid.NewGuid():N}");
        await PostJsonAsync<WorkflowConfigurationValidationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/validate",
            new { },
            $"wfrole-validate-{Guid.NewGuid():N}");
        var published = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/publish",
            new { },
            $"wfrole-publish-{Guid.NewGuid():N}");
        published.PublishedWorkflowVersionId.ShouldNotBeNullOrWhiteSpace();

        var instance = await PostJsonAsync<WorkflowInstanceDto>(
            admin,
            "/api/v1/workflow-instances",
            new StartWorkflowInstanceRequest(
                Guid.Parse(published.PublishedWorkflowDefinitionId!),
                Guid.Parse(published.PublishedWorkflowVersionId!),
                Guid.Parse(createdProject.Id),
                $"wfrole-start-{Guid.NewGuid():N}"),
            null);

        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        var researchTask = await FindTaskAsync(researcher.Client, instance.Id, "RD_INITIATE");
        await PostJsonAsync<WorkflowTaskDto>(
            researcher,
            $"/api/v1/workflow-tasks/{researchTask.Id}/actions/decide",
            new TaskDecisionRequest(
                AIDT.Domain.Workflows.WorkflowDecision.Approve,
                researchTask.Version,
                "研发提交",
                $"rd-decide-{Guid.NewGuid():N}"),
            null);

        var roles = await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles");
        var managerRole = roles.Single(item => item.Code == "business-manager-reviewer");
        var currentMembers = await GetAsync<IReadOnlyList<RoleMemberDto>>(
            admin.Client,
            $"/api/v1/iam/roles/{managerRole.Id}/members");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                currentMembers
                    .Where(member => member.IsActive && member.UserName != "demo.pm")
                    .Select(member => new SetRoleMemberItem(
                        Guid.Parse(member.UserId),
                        member.ScopeType,
                        member.ScopeReference,
                        true))
                    .ToList(),
                managerRole.Version),
            $"role-history-{Guid.NewGuid():N}");

        using var manager = await CreateAuthenticatedClientAsync("demo.pm");
        var managerTask = await FindTaskAsync(manager.Client, instance.Id, "MANAGER_REVIEW");
        managerTask.AssigneeStrategy.ShouldBe("BusinessRole");
        managerTask.BusinessRoleCode.ShouldBe("business-manager-reviewer");
        managerTask.CandidateUserIds.ShouldNotBeNull();
        var invalidTransfer = await PostJsonRawAsync(
            admin,
            $"/api/v1/workflow-tasks/{managerTask.Id}/actions/transfer",
            new TransferWorkflowTaskRequest(
                Guid.Parse(await GetUserIdAsync("demo.qa")),
                managerTask.Version,
                "验证无业务角色资格的目标被拒绝",
                $"transfer-invalid-{Guid.NewGuid():N}"),
            null);
        invalidTransfer.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var transferred = await PostJsonAsync<WorkflowTaskDto>(
            admin,
            $"/api/v1/workflow-tasks/{managerTask.Id}/actions/transfer",
            new TransferWorkflowTaskRequest(
                Guid.Parse(securityId),
                managerTask.Version,
                "验证备份转交保留原责任人",
                $"transfer-out-{Guid.NewGuid():N}"),
            null);
        transferred.OriginalAssigneeId.ShouldBe(managerTask.AssigneeId);
        transferred.AssigneeId.ShouldBe(securityId);
        var transferredBack = await PostJsonAsync<WorkflowTaskDto>(
            admin,
            $"/api/v1/workflow-tasks/{managerTask.Id}/actions/transfer",
            new TransferWorkflowTaskRequest(
                Guid.Parse(managerTask.AssigneeId),
                transferred.Version,
                "转回原项目经理",
                $"transfer-back-{Guid.NewGuid():N}"),
            null);
        transferredBack.AssigneeId.ShouldBe(managerTask.AssigneeId);
        transferredBack.OriginalAssigneeId.ShouldBe(managerTask.AssigneeId);
        await PostJsonAsync<WorkflowTaskDto>(
            manager,
            $"/api/v1/workflow-tasks/{managerTask.Id}/actions/decide",
            new TaskDecisionRequest(
                AIDT.Domain.Workflows.WorkflowDecision.Approve,
                transferredBack.Version,
                "项目经理岗位审批通过",
                $"pm-decide-{Guid.NewGuid():N}"),
            null);

        using var quality = await CreateAuthenticatedClientAsync("demo.qa");
        var qualityTask = await FindTaskAsync(quality.Client, instance.Id, "QA_REVIEW");
        await PostJsonAsync<WorkflowTaskDto>(
            quality,
            $"/api/v1/workflow-tasks/{qualityTask.Id}/actions/decide",
            new TaskDecisionRequest(
                AIDT.Domain.Workflows.WorkflowDecision.Approve,
                qualityTask.Version,
                "质量复核通过",
                $"qa-decide-{Guid.NewGuid():N}"),
            null);

        var completed = await GetAsync<WorkflowInstanceDto>(
            admin.Client,
            $"/api/v1/workflow-instances/{instance.Id}");
        completed.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowInstanceStatus.Completed);
    }

    [SqlIntegrationFact]
    public async Task Iam_PreventsSelfPromotionAndRequiresSecondAdminForHighRiskPermission()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var roles = await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles");
        var managerRole = roles.Single(item => item.Code == "business-manager-reviewer");
        var pmId = await GetUserIdAsync("demo.pm");

        using var manager = await CreateAuthenticatedClientAsync("demo.pm");
        var selfAssign = await PutJsonRawAsync(
            manager,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(pmId),
                        "organization",
                        "organization",
                        true)
                ],
                managerRole.Version),
            $"iam-self-{Guid.NewGuid():N}");
        selfAssign.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var researcherId = await GetUserIdAsync("demo.researcher");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(researcherId),
                        "organization",
                        "organization",
                        true)
                ],
                managerRole.Version),
            $"iam-used-role-{Guid.NewGuid():N}");
        var roleAfterAssignment = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == managerRole.Id);
        var deactivateUsedRole = await PostJsonRawAsync(
            admin,
            $"/api/v1/iam/roles/{managerRole.Id}/actions/deactivate",
            new { },
            null);
        deactivateUsedRole.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        roleAfterAssignment.MemberCount.ShouldBeGreaterThan(0);

        var roleCode = $"HIGH-RISK-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var role = await PostJsonAsync<RoleDto>(
            admin,
            "/api/v1/iam/roles",
            new CreateRoleRequest(roleCode, "高风险权限验收角色", "Business", null),
            $"iam-role-{Guid.NewGuid():N}");
        var permissions = await GetAsync<IReadOnlyList<PermissionCatalogItemDto>>(
            admin.Client,
            "/api/v1/iam/permissions");
        var highRiskPermission = permissions.Single(item => item.Code == "identity-admin-manage");

        var requestResponse = await PutJsonAsync<RoleDto>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest([Guid.Parse(highRiskPermission.Id)], role.Version),
            $"iam-high-{Guid.NewGuid():N}");
        requestResponse.PermissionIds.ShouldNotBeNull().ShouldNotContain(highRiskPermission.Id);

        var pending = await GetAsync<IReadOnlyList<AccessChangeRequestDto>>(
            admin.Client,
            "/api/v1/iam/access-requests?status=Pending");
        var accessRequest = pending.Single(item => item.TargetRoleId == role.Id);

        var selfReview = await PostJsonRawAsync(
            admin,
            $"/api/v1/iam/access-requests/{accessRequest.Id}/actions/approve",
            new ReviewAccessChangeRequest("自审"),
            $"iam-self-review-{Guid.NewGuid():N}");
        selfReview.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var security = await CreateAuthenticatedClientAsync("demo.security");
        var approved = await PostJsonAsync<AccessChangeRequestDto>(
            security,
            $"/api/v1/iam/access-requests/{accessRequest.Id}/actions/approve",
            new ReviewAccessChangeRequest("第二管理员复核通过"),
            $"iam-review-{Guid.NewGuid():N}");
        approved.Status.ShouldBe("Applied");

        var updatedRole = (await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles"))
            .Single(item => item.Id == role.Id);
        updatedRole.PermissionIds.ShouldNotBeNull().ShouldContain(highRiskPermission.Id);
    }

    [SqlIntegrationFact]
    public async Task IamRoleGrantAndRevoke_KeepMenuAndApiPermissionsConsistent()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var permissions = await GetAsync<IReadOnlyList<PermissionCatalogItemDto>>(
            admin.Client,
            "/api/v1/iam/permissions");
        var identityRead = permissions.Single(item => item.Code == "identity-admin-read");
        var roleCode = $"IAM-MENU-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        var role = await PostJsonAsync<RoleDto>(
            admin,
            "/api/v1/iam/roles",
            new CreateRoleRequest(roleCode, "IAM 菜单一致性验收", "Business", null),
            $"iam-menu-role-{Guid.NewGuid():N}");
        role = await PutJsonAsync<RoleDto>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest([Guid.Parse(identityRead.Id)], role.Version),
            $"iam-menu-permission-{Guid.NewGuid():N}");
        var researcherId = await GetUserIdAsync("demo.researcher");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(researcherId),
                        "organization",
                        "organization",
                        true)
                ],
                role.Version),
            $"iam-menu-member-{Guid.NewGuid():N}");

        using (var researcher = await CreateAuthenticatedClientAsync("demo.researcher"))
        {
            var current = await GetAsync<CurrentNavigationDto>(
                researcher.Client,
                "/api/v1/navigation/current");
            current.Groups.SelectMany(group => group.Items)
                .ShouldContain(item => item.ItemKey == "IAM_NAVIGATION");
            var snapshot = await researcher.Client.GetAsync("/api/v1/iam/snapshot");
            snapshot.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var roleBeforeRevoke = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == role.Id);
        await PutJsonAsync<RoleDto>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest([], roleBeforeRevoke.Version),
            $"iam-menu-revoke-{Guid.NewGuid():N}");

        using (var researcher = await CreateAuthenticatedClientAsync("demo.researcher"))
        {
            var current = await GetAsync<CurrentNavigationDto>(
                researcher.Client,
                "/api/v1/navigation/current");
            current.Groups.SelectMany(group => group.Items)
                .ShouldNotContain(item => item.ItemKey == "IAM_NAVIGATION");
            var snapshot = await researcher.Client.GetAsync("/api/v1/iam/snapshot");
            snapshot.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    [SqlIntegrationFact]
    public async Task LegacyRoleUsage_IsCountedAndBlocksDeactivation()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var roles = await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles");
        foreach (var roleCode in new[] { "rd-engineer", "project-manager" })
        {
            var role = roles.Single(item => item.Code == roleCode);
            role.MemberCount.ShouldBeGreaterThan(0, $"{roleCode} 的 legacy 有效成员必须计入统计。");
            var response = await PostJsonRawAsync(
                admin,
                $"/api/v1/iam/roles/{role.Id}/actions/deactivate",
                new { },
                null);
            response.StatusCode.ShouldBe(
                HttpStatusCode.Conflict,
                $"{roleCode} 仍被有效用户使用时不能停用。");
        }
    }

    [SqlIntegrationFact]
    public async Task ProjectManager_IamWritesStayInsideAuthorizedDepartmentAndCannotDelegateBeyondSelf()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var manager = await CreateAuthenticatedClientAsync("demo.pm");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var qa = Flatten(tree).Single(item => item.Code == "QA");
        var roles = await GetAsync<IReadOnlyList<RoleDto>>(admin.Client, "/api/v1/iam/roles");
        var managerRole = roles.Single(item => item.Code == "business-manager-reviewer");
        var researcherId = Guid.Parse(await GetUserIdAsync("demo.researcher"));
        var qualityId = Guid.Parse(await GetUserIdAsync("demo.qa"));

        var organizationGrant = await PutJsonRawAsync(
            manager,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                [new SetRoleMemberItem(researcherId, "organization", "organization", true)],
                managerRole.Version),
            $"pm-org-{Guid.NewGuid():N}");
        organizationGrant.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var currentMembers = await GetAsync<IReadOnlyList<RoleMemberDto>>(
            admin.Client,
            $"/api/v1/iam/roles/{managerRole.Id}/members");
        var seedMembers = currentMembers
            .Where(item => item.IsActive)
            .Select(item => new SetRoleMemberItem(
                Guid.Parse(item.UserId),
                item.ScopeType,
                item.ScopeReference,
                true))
            .Append(new SetRoleMemberItem(qualityId, "department", qa.Id, true))
            .GroupBy(item => (item.UserId, item.ScopeType, item.ScopeReference))
            .Select(group => group.First())
            .ToArray();
        var seeded = await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(seedMembers, managerRole.Version),
            $"pm-seed-{Guid.NewGuid():N}");
        var roleAfterSeed = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == managerRole.Id);

        var crossDepartment = await PutJsonRawAsync(
            manager,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                [
                    .. seeded
                        .Where(item => item.IsActive)
                        .Select(item => new SetRoleMemberItem(
                            Guid.Parse(item.UserId),
                            item.ScopeType,
                            item.ScopeReference,
                            true)),
                    new SetRoleMemberItem(
                        researcherId,
                        "department",
                        qa.Id,
                        true)
                ],
                roleAfterSeed.Version),
            $"pm-cross-{Guid.NewGuid():N}");
        crossDepartment.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            manager,
            $"/api/v1/iam/roles/{managerRole.Id}/members",
            new SetRoleMembersRequest(
                [new SetRoleMemberItem(researcherId, "department", rnd.Id, true)],
                roleAfterSeed.Version),
            $"pm-in-scope-{Guid.NewGuid():N}");
        var afterScopeReplacement = await GetAsync<IReadOnlyList<RoleMemberDto>>(
            admin.Client,
            $"/api/v1/iam/roles/{managerRole.Id}/members");
        afterScopeReplacement.ShouldContain(item =>
            item.UserId == qualityId.ToString("D")
            && item.ScopeReference == qa.Id
            && item.IsActive,
            "部门负责人替换本部门成员时不能撤销其他部门成员。");

        var roleAfterReplacement = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == managerRole.Id);
        var permissionChange = await PutJsonRawAsync(
            manager,
            $"/api/v1/iam/roles/{managerRole.Id}/permissions",
            new SetRolePermissionsRequest([], roleAfterReplacement.Version),
            $"pm-permission-{Guid.NewGuid():N}");
        permissionChange.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [SqlIntegrationFact]
    public async Task BusinessRoleDepartmentScope_RejectsMembersFromAnotherDepartment()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var qa = Flatten(tree).Single(item => item.Code == "QA");
        var roleCode = $"SCOPE-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
        var role = await PostJsonAsync<RoleDto>(
            admin,
            "/api/v1/iam/roles",
            new CreateRoleRequest(roleCode, "部门范围候选验收角色", "Business", null),
            $"scope-role-{Guid.NewGuid():N}");
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(await GetUserIdAsync("demo.qa")),
                        "department",
                        qa.Id,
                        true)
                ],
                role.Version),
            $"scope-member-{Guid.NewGuid():N}");

        var configurationKey = $"SCOPE-E2E-{DateTimeOffset.UtcNow:HHmmssfff}";
        var configuration = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            "/api/v1/configurations",
            new CreateWorkflowConfigurationRequest(
                configurationKey,
                "研发节点不得使用质量角色成员",
                "验证 department scope 与 workflowDepartmentId 一致",
                Guid.Parse(rnd.Id),
                "ANY",
                "RND_EXECUTE",
                [
                    new WorkflowConfigurationNodeRequest(
                        "RND_EXECUTE",
                        "研发执行",
                        null,
                        null,
                        true,
                        null,
                        "BusinessRole",
                        roleCode,
                        null)
                ]),
            $"scope-config-{Guid.NewGuid():N}");
        var validation = await PostJsonAsync<WorkflowConfigurationValidationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/validate",
            new { },
            $"scope-validate-{Guid.NewGuid():N}");
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(message =>
            message.Contains("没有可解析成员", StringComparison.Ordinal));

        var roleVersion = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == role.Id).Version;
        await PutJsonAsync<IReadOnlyList<RoleMemberDto>>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/members",
            new SetRoleMembersRequest(
                [
                    new SetRoleMemberItem(
                        Guid.Parse(await GetUserIdAsync("demo.researcher")),
                        "organization",
                        "organization",
                        true)
                ],
                roleVersion),
            $"scope-org-member-{Guid.NewGuid():N}");
        var validAfterOrganizationGrant = await PostJsonAsync<WorkflowConfigurationValidationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/validate",
            new { },
            $"scope-valid-{Guid.NewGuid():N}");
        validAfterOrganizationGrant.IsValid.ShouldBeTrue();
    }

    [SqlIntegrationFact]
    public async Task AgentNode_CompletesLowRiskNodeOrBlocksPermanentHumanBoundary()
    {
        using var gatewayFactory = CreateFactoryWithRuleGateway();
        using var admin = await CreateAuthenticatedClientAsync("demo.admin", gatewayFactory);
        var profiles = await GetAsync<IReadOnlyList<AgentProfileDto>>(admin.Client, "/api/v1/agents");
        var profile = profiles.First(item =>
            item.IsActive
            && item.AutonomyLevel is AIDT.Domain.Agents.AgentAutonomyLevel.A2
                or AIDT.Domain.Agents.AgentAutonomyLevel.A3
                or AIDT.Domain.Agents.AgentAutonomyLevel.A4
            && item.RiskLimit >= AIDT.Domain.Agents.AgentRiskLevel.Low);
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var pmId = await GetUserIdAsync("demo.pm");
        async Task<ProjectDto> CreateProjectAsync(
            string codePrefix,
            string projectName,
            string objective)
        {
            return await PostJsonAsync<ProjectDto>(
                admin,
                "/api/v1/projects",
                new CreateProjectRequest(
                    $"{codePrefix}-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                    projectName,
                    Guid.Parse(pmId),
                    Guid.Parse(rnd.Id),
                    null,
                    null,
                    objective,
                    "验收",
                    "Internal"),
                $"agent-project-{Guid.NewGuid():N}");
        }

        var businessObject = await CreateProjectAsync(
            "AGENT",
            "Agent 节点闭环验收",
            "验证 Agent 节点自动完成与永久人工边界");

        async Task<WorkflowConfigurationDto> CreateConfigurationAsync(string nodeCode)
        {
            var configuration = await PostJsonAsync<WorkflowConfigurationDto>(
                admin,
                "/api/v1/configurations",
                new CreateWorkflowConfigurationRequest(
                    $"AGENT-{nodeCode}-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                    $"Agent 节点 {nodeCode}",
                    "验证 Agent 节点闭环",
                    Guid.Parse(rnd.Id),
                    "PROJECT",
                    nodeCode,
                    [
                        new WorkflowConfigurationNodeRequest(
                            nodeCode,
                            nodeCode,
                            null,
                            null,
                            true,
                            null,
                            "Agent",
                            null,
                            Guid.Parse(profile.Id))
                    ]),
                $"agent-config-{Guid.NewGuid():N}");
            await PostJsonAsync<WorkflowConfigurationValidationDto>(
                admin,
                $"/api/v1/configurations/{configuration.Id}/actions/validate",
                new { },
                $"agent-validate-{Guid.NewGuid():N}");
            return await PostJsonAsync<WorkflowConfigurationDto>(
                admin,
                $"/api/v1/configurations/{configuration.Id}/actions/publish",
                new { },
                $"agent-publish-{Guid.NewGuid():N}");
        }

        var automaticConfiguration = await CreateConfigurationAsync("AUTO_LOW_RISK");
        var automaticInstance = await PostJsonAsync<WorkflowInstanceDto>(
            admin,
            "/api/v1/workflow-instances",
            new StartWorkflowInstanceRequest(
                Guid.Parse(automaticConfiguration.PublishedWorkflowDefinitionId!),
                Guid.Parse(automaticConfiguration.PublishedWorkflowVersionId!),
                Guid.Parse(businessObject.Id),
                $"agent-start-{Guid.NewGuid():N}"),
            null);
        await PostJsonAsync<SlaProcessingResultDto>(
            admin,
            "/api/v1/workflow-sla/actions/run",
            new { },
            null);
        var automaticTasks = await GetAsync<PagedResult<WorkflowTaskDto>>(
            admin.Client,
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=true");
        var automaticTask = automaticTasks.Items.Single(item =>
            item.WorkflowInstanceId == automaticInstance.Id);
        automaticTask.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Approved);
        automaticTask.DecisionMode.ShouldBe("AgentProxy");
        automaticTask.AgentRunId.ShouldNotBeNullOrWhiteSpace();
        var automaticRun = await GetAsync<PagedResult<AgentRunDto>>(
            admin.Client,
            "/api/v1/agents/runs?pageSize=100");
        var run = automaticRun.Items.Single(item => item.Id == automaticTask.AgentRunId);
        run.Status.ShouldBe(AIDT.Domain.Agents.AgentRunStatus.Completed);
        var calls = await GetAsync<IReadOnlyList<AgentGatewayCallDto>>(
            admin.Client,
            $"/api/v1/agents/runs/{run.Id}/gateway-calls");
        calls.ShouldNotBeEmpty();
        calls.ShouldAllBe(item => item.ExternalModelCalled == false);

        var blockedBusinessObject = await CreateProjectAsync(
            "AGENT-BLOCK",
            "Agent 永久人工边界验收",
            "验证 Agent 节点高风险阻断");
        var blockedConfiguration = await CreateConfigurationAsync("QUALITY_RELEASE");
        var blockedInstance = await PostJsonAsync<WorkflowInstanceDto>(
            admin,
            "/api/v1/workflow-instances",
            new StartWorkflowInstanceRequest(
                Guid.Parse(blockedConfiguration.PublishedWorkflowDefinitionId!),
                Guid.Parse(blockedConfiguration.PublishedWorkflowVersionId!),
                Guid.Parse(blockedBusinessObject.Id),
                $"agent-blocked-start-{Guid.NewGuid():N}"),
            null);
        await PostJsonAsync<SlaProcessingResultDto>(
            admin,
            "/api/v1/workflow-sla/actions/run",
            new { },
            null);
        var blockedTasks = await GetAsync<PagedResult<WorkflowTaskDto>>(
            admin.Client,
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=true");
        var blockedTask = blockedTasks.Items.Single(item =>
            item.WorkflowInstanceId == blockedInstance.Id);
        blockedTask.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Pending);
        blockedTask.DecisionMode.ShouldBe("AgentBlocked");
        blockedTask.AgentRunId.ShouldBeNull();
        blockedTask.AssigneeId.ShouldNotBeNullOrWhiteSpace();
    }

    [SqlIntegrationFact]
    public async Task AgentNode_RequiresHumanConfirmationAndServiceIdentityCannotSelfConfirm()
    {
        using var gatewayFactory = CreateFactoryWithRuleGateway();
        using var admin = await CreateAuthenticatedClientAsync("demo.admin", gatewayFactory);
        var profiles = await GetAsync<IReadOnlyList<AgentProfileDto>>(admin.Client, "/api/v1/agents");
        var profile = profiles.Single(item =>
            item.IsActive
            && item.AutonomyLevel == AIDT.Domain.Agents.AgentAutonomyLevel.A1);
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var project = await PostJsonAsync<ProjectDto>(
            admin,
            "/api/v1/projects",
            new CreateProjectRequest(
                $"AGENT-CONFIRM-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                "Agent 人工确认验收",
                Guid.Parse(await GetUserIdAsync("demo.pm")),
                Guid.Parse(rnd.Id),
                null,
                null,
                "验证 Agent 结果必须由真人确认",
                "验收",
                "Internal"),
            $"agent-confirm-object-{Guid.NewGuid():N}");
        var configuration = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            "/api/v1/configurations",
            new CreateWorkflowConfigurationRequest(
                $"AGENT-CONFIRM-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                "Agent 人工确认节点",
                "验证 A1 Agent 不得自确认",
                Guid.Parse(rnd.Id),
                "PROJECT",
                "AGENT_DRAFT",
                [
                    new WorkflowConfigurationNodeRequest(
                        "AGENT_DRAFT",
                        "Agent 生成处理结果",
                        null,
                        null,
                        true,
                        null,
                        "Agent",
                        null,
                        Guid.Parse(profile.Id))
                ]),
            $"agent-confirm-config-{Guid.NewGuid():N}");
        await PostJsonAsync<WorkflowConfigurationValidationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/validate",
            new { },
            $"agent-confirm-validate-{Guid.NewGuid():N}");
        var published = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            $"/api/v1/configurations/{configuration.Id}/actions/publish",
            new { },
            $"agent-confirm-publish-{Guid.NewGuid():N}");
        var instance = await PostJsonAsync<WorkflowInstanceDto>(
            admin,
            "/api/v1/workflow-instances",
            new StartWorkflowInstanceRequest(
                Guid.Parse(published.PublishedWorkflowDefinitionId!),
                Guid.Parse(published.PublishedWorkflowVersionId!),
                Guid.Parse(project.Id),
                $"agent-confirm-start-{Guid.NewGuid():N}"),
            null);
        await PostJsonAsync<SlaProcessingResultDto>(
            admin,
            "/api/v1/workflow-sla/actions/run",
            new { },
            null);
        var tasks = await GetAsync<PagedResult<WorkflowTaskDto>>(
            admin.Client,
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=true");
        var task = tasks.Items.Single(item => item.WorkflowInstanceId == instance.Id);
        task.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Pending);
        task.DecisionMode.ShouldBe("AwaitingAgentConfirmation");
        task.AgentRunId.ShouldNotBeNullOrWhiteSpace();
        var runs = await GetAsync<PagedResult<AgentRunDto>>(
            admin.Client,
            "/api/v1/agents/runs?pageSize=100");
        var run = runs.Items.Single(item => item.Id == task.AgentRunId);
        run.Status.ShouldBe(AIDT.Domain.Agents.AgentRunStatus.AwaitingConfirmation);

        using var agentLoginClient = _factory.CreateClient();
        using var serviceLogin = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest("agent.demo"))
        };
        serviceLogin.Headers.Add(
            "X-CSRF-TOKEN",
            await GetCsrfTokenAsync(agentLoginClient));
        (await agentLoginClient.SendAsync(serviceLogin)).StatusCode.ShouldBe(
            HttpStatusCode.Forbidden);

        var confirmationId = Guid.NewGuid();
        var confirmed = await PostJsonAsync<AgentRunDto>(
            admin,
            $"/api/v1/agents/runs/{run.Id}/actions/confirm-proxy",
            new ConfirmAgentProxyRequest(confirmationId),
            null);
        confirmed.Status.ShouldBe(AIDT.Domain.Agents.AgentRunStatus.Completed);
        var confirmedTask = (await GetAsync<PagedResult<WorkflowTaskDto>>(
            admin.Client,
            "/api/v1/workflow-tasks?pageSize=100&includeTestData=true"))
            .Items.Single(item => item.Id == task.Id);
        confirmedTask.Status.ShouldBe(AIDT.Domain.Workflows.WorkflowTaskStatus.Approved);
        confirmedTask.DecisionMode.ShouldBe("AgentProxy");
        var repeated = await PostJsonAsync<AgentRunDto>(
            admin,
            $"/api/v1/agents/runs/{run.Id}/actions/confirm-proxy",
            new ConfirmAgentProxyRequest(confirmationId),
            null);
        repeated.Status.ShouldBe(AIDT.Domain.Agents.AgentRunStatus.Completed);
    }

    [SqlIntegrationFact]
    public async Task DepartmentManager_ProjectOwnerAndInitiatorManager_ResolveEligibleUsersAndRejectMissingManagers()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        using var researcher = await CreateAuthenticatedClientAsync("demo.researcher");
        using var security = await CreateAuthenticatedClientAsync("demo.security");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var pmId = await GetUserIdAsync("demo.pm");
        var researcherId = await GetUserIdAsync("demo.researcher");
        var adminId = await GetUserIdAsync("demo.admin");
        var originalManagerState = new Dictionary<string, bool>();
        foreach (var userId in new[] { pmId, researcherId, adminId })
        {
            var detail = await GetAsync<IamUserDetailDto>(
                admin.Client,
                $"/api/v1/iam/users/{userId}");
            originalManagerState[userId] = detail.Memberships.Any(item =>
                item.DepartmentId == rnd.Id && item.IsActive && item.IsManager);
        }

        async Task MarkDepartmentManagerAsync(
            AuthenticatedClient actor,
            string userId,
            bool isManager)
        {
            var detail = await GetAsync<IamUserDetailDto>(
                actor.Client,
                $"/api/v1/iam/users/{userId}");
            await PutJsonAsync<IamUserDetailDto>(
                actor,
                $"/api/v1/iam/users/{userId}/memberships",
                new SetUserMembershipsRequest(
                    detail.Memberships
                        .Where(item => item.IsActive)
                        .Select(item => new SetUserMembershipItem(
                            Guid.Parse(item.DepartmentId),
                            item.IsPrimary,
                            item.DepartmentId == rnd.Id ? isManager : item.IsManager,
                            true))
                        .ToList(),
                    detail.User.Version),
                null);
        }

        await MarkDepartmentManagerAsync(admin, pmId, true);
        await MarkDepartmentManagerAsync(admin, researcherId, false);
        await MarkDepartmentManagerAsync(security, adminId, false);
        await PutJsonAsync<DepartmentTreeNodeDto>(
            admin,
            $"/api/v1/iam/departments/{rnd.Id}",
            new UpdateIamDepartmentRequest(
                rnd.Version,
                rnd.Name,
                rnd.ParentId,
                "Department",
                Guid.Parse(pmId),
                rnd.SortOrder,
                true),
            null);
        async Task<ProjectDto> CreateProjectAsync()
        {
            return await PostJsonAsync<ProjectDto>(
                admin,
                "/api/v1/projects",
                new CreateProjectRequest(
                    $"STRATEGY-{Guid.NewGuid():N}"[..28].ToUpperInvariant(),
                    "处理人策略验收",
                    Guid.Parse(pmId),
                    Guid.Parse(rnd.Id),
                    null,
                    null,
                    "覆盖部门负责人、项目负责人和发起人主管",
                    "验收",
                    "Internal"),
                $"strategy-object-{Guid.NewGuid():N}");
        }

        async Task<WorkflowInstanceDto> StartStrategyAsync(
            AuthenticatedClient starter,
            string strategy)
        {
            var project = await CreateProjectAsync();
            var configuration = await PostJsonAsync<WorkflowConfigurationDto>(
                admin,
                "/api/v1/configurations",
                new CreateWorkflowConfigurationRequest(
                    $"STRATEGY-{strategy}-{Guid.NewGuid():N}"[..32].ToUpperInvariant(),
                    $"{strategy} 处理人策略",
                    "验证动态候选解析",
                    Guid.Parse(rnd.Id),
                    "PROJECT",
                    "STRATEGY_NODE",
                    [
                        new WorkflowConfigurationNodeRequest(
                            "STRATEGY_NODE",
                            strategy,
                            null,
                            null,
                            true,
                            null,
                            strategy,
                            null,
                            null)
                    ]),
                $"strategy-config-{Guid.NewGuid():N}");
            await PostJsonAsync<WorkflowConfigurationValidationDto>(
                admin,
                $"/api/v1/configurations/{configuration.Id}/actions/validate",
                new { },
                $"strategy-validate-{Guid.NewGuid():N}");
            var published = await PostJsonAsync<WorkflowConfigurationDto>(
                admin,
                $"/api/v1/configurations/{configuration.Id}/actions/publish",
                new { },
                $"strategy-publish-{Guid.NewGuid():N}");
            return await PostJsonAsync<WorkflowInstanceDto>(
                starter,
                "/api/v1/workflow-instances",
                new StartWorkflowInstanceRequest(
                    Guid.Parse(published.PublishedWorkflowDefinitionId!),
                    Guid.Parse(published.PublishedWorkflowVersionId!),
                    Guid.Parse(project.Id),
                    $"strategy-start-{Guid.NewGuid():N}"),
                null);
        }

        foreach (var strategy in new[]
                 {
                     "DepartmentManager",
                     "ProjectOwner",
                     "InitiatorManager"
                 })
        {
            var instance = await StartStrategyAsync(researcher, strategy);
            var task = await FindTaskAsync(admin.Client, instance.Id, "STRATEGY_NODE");
            task.AssigneeId.ShouldBe(pmId);
            task.AssigneeStrategy.ShouldBe(strategy);
            task.CandidateUserIds.ShouldNotBeNull().ShouldContain(pmId);
        }

        await MarkDepartmentManagerAsync(admin, pmId, false);
        await MarkDepartmentManagerAsync(admin, researcherId, false);
        await MarkDepartmentManagerAsync(security, adminId, false);
        var latestRnd = Flatten(await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree")).Single(item => item.Code == "RND");
        await PutJsonAsync<DepartmentTreeNodeDto>(
            admin,
            $"/api/v1/iam/departments/{latestRnd.Id}",
            new UpdateIamDepartmentRequest(
                latestRnd.Version,
                latestRnd.Name,
                latestRnd.ParentId,
                "Department",
                null,
                latestRnd.SortOrder,
                true),
            null);
        var missingManagerConfiguration = await PostJsonAsync<WorkflowConfigurationDto>(
            admin,
            "/api/v1/configurations",
            new CreateWorkflowConfigurationRequest(
                $"INIT-MISSING-{Guid.NewGuid():N}"[..30].ToUpperInvariant(),
                "发起人主管缺失验收",
                "部门无负责人时必须阻断",
                Guid.Parse(rnd.Id),
                "PROJECT",
                "MANAGER",
                [
                    new WorkflowConfigurationNodeRequest(
                        "MANAGER",
                        "主管审核",
                        null,
                        null,
                        true,
                        null,
                        "InitiatorManager",
                        null,
                        null)
                ]),
            $"missing-manager-config-{Guid.NewGuid():N}");
        var missingManagerValidation = await PostJsonAsync<WorkflowConfigurationValidationDto>(
            admin,
            $"/api/v1/configurations/{missingManagerConfiguration.Id}/actions/validate",
            new { },
            $"missing-manager-validate-{Guid.NewGuid():N}");
        missingManagerValidation.IsValid.ShouldBeFalse();

        foreach (var userId in new[] { pmId, researcherId })
        {
            await MarkDepartmentManagerAsync(admin, userId, originalManagerState[userId]);
        }
        await MarkDepartmentManagerAsync(security, adminId, originalManagerState[adminId]);
        var restoredRnd = Flatten(await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree")).Single(item => item.Code == "RND");
        await PutJsonAsync<DepartmentTreeNodeDto>(
            admin,
            $"/api/v1/iam/departments/{restoredRnd.Id}",
            new UpdateIamDepartmentRequest(
                restoredRnd.Version,
                restoredRnd.Name,
                restoredRnd.ParentId,
                "Department",
                rnd.ManagerUserId == null ? null : Guid.Parse(rnd.ManagerUserId),
                restoredRnd.SortOrder,
                true),
            null);
    }

    [SqlIntegrationFact]
    public async Task HighRiskAccessRequest_RequiresOpinionAndSupportsRejection()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var role = await PostJsonAsync<RoleDto>(
            admin,
            "/api/v1/iam/roles",
            new CreateRoleRequest(
                $"REJECT-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                "高风险驳回验收角色",
                "Business",
                null),
            $"reject-role-{Guid.NewGuid():N}");
        var permissions = await GetAsync<IReadOnlyList<PermissionCatalogItemDto>>(
            admin.Client,
            "/api/v1/iam/permissions");
        var highRiskPermission = permissions.Single(item => item.Code == "identity-admin-manage");
        await PutJsonAsync<RoleDto>(
            admin,
            $"/api/v1/iam/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest([Guid.Parse(highRiskPermission.Id)], role.Version),
            $"reject-request-{Guid.NewGuid():N}");
        var pending = await GetAsync<IReadOnlyList<AccessChangeRequestDto>>(
            admin.Client,
            "/api/v1/iam/access-requests?status=Pending");
        var accessRequest = pending.Single(item => item.TargetRoleId == role.Id);
        using var security = await CreateAuthenticatedClientAsync("demo.security");
        var missingOpinion = await PostJsonRawAsync(
            security,
            $"/api/v1/iam/access-requests/{accessRequest.Id}/actions/reject",
            new ReviewAccessChangeRequest(""),
            $"reject-empty-{Guid.NewGuid():N}");
        missingOpinion.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var rejected = await PostJsonAsync<AccessChangeRequestDto>(
            security,
            $"/api/v1/iam/access-requests/{accessRequest.Id}/actions/reject",
            new ReviewAccessChangeRequest("不符合最小权限原则"),
            $"reject-ok-{Guid.NewGuid():N}");
        rejected.Status.ShouldBe("Rejected");
        rejected.ReviewComment.ShouldBe("不符合最小权限原则");
        var updatedRole = (await GetAsync<IReadOnlyList<RoleDto>>(
            admin.Client,
            "/api/v1/iam/roles")).Single(item => item.Id == role.Id);
        updatedRole.PermissionIds.ShouldNotBeNull().ShouldNotContain(highRiskPermission.Id);
    }

    [SqlIntegrationFact]
    public async Task IamUsers_ServerPaginationAndSearchCoverMoreThanFiftyPeople()
    {
        using var admin = await CreateAuthenticatedClientAsync("demo.admin");
        var tree = await GetAsync<IReadOnlyList<DepartmentTreeNodeDto>>(
            admin.Client,
            "/api/v1/iam/organization-tree");
        var rnd = Flatten(tree).Single(item => item.Code == "RND");
        var prefix = $"page{Guid.NewGuid():N}"[..12];
        for (var index = 1; index <= 55; index++)
        {
            await PostJsonAsync<IamUserDetailDto>(
                admin,
                "/api/v1/iam/users",
                new InviteUserRequest(
                    $"{prefix}.{index:D3}",
                    $"分页验收 {index:D3}",
                    Guid.Parse(rnd.Id),
                    true,
                    false),
                $"iam-page-{Guid.NewGuid():N}");
        }

        var secondPage = await GetAsync<PagedResult<IamUserListItemDto>>(
            admin.Client,
            $"/api/v1/iam/users?page=2&pageSize=20&search={Uri.EscapeDataString(prefix)}");
        secondPage.Page.ShouldBe(2);
        secondPage.PageSize.ShouldBe(20);
        secondPage.Total.ShouldBe(55);
        secondPage.Items.Count.ShouldBe(20);
        var lastPage = await GetAsync<PagedResult<IamUserListItemDto>>(
            admin.Client,
            $"/api/v1/iam/users?page=3&pageSize=20&search={Uri.EscapeDataString(prefix)}");
        lastPage.Items.Count.ShouldBe(15);
        lastPage.Items.ShouldAllBe(item => item.UserName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static async Task<WorkflowTaskDto> FindTaskAsync(HttpClient client, string instanceId, string nodeCode)
    {
        var page = await GetAsync<PagedResult<WorkflowTaskDto>>(
            client,
            "/api/v1/workflow-tasks?pageSize=100");
        return page.Items.Single(item =>
            item.WorkflowInstanceId == instanceId && item.NodeCode == nodeCode);
    }

    private async Task<string> GetUserIdAsync(string userName)
    {
        using var client = await CreateAuthenticatedClientAsync("demo.admin");
        var users = await GetAsync<PagedResult<IamUserListItemDto>>(
            client.Client,
            $"/api/v1/iam/users?search={Uri.EscapeDataString(userName)}");
        return users.Items.Single(item => item.UserName == userName).Id;
    }

    private async Task<AuthenticatedClient> CreateAuthenticatedClientAsync(
        string userName,
        WebApplicationFactory<Program>? factory = null)
    {
        var client = (factory ?? _factory).CreateClient();
        var csrf = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/demo-login")
        {
            Content = JsonContent.Create(new DemoLoginRequest(userName))
        };
        login.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(login);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return new AuthenticatedClient(client, await GetCsrfTokenAsync(client));
    }

    private WebApplicationFactory<Program> CreateFactoryWithRuleGateway()
    {
        var gatewayClient = new HttpClient(new RuleGatewayHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1:5100")
        };
        var factory = NSubstitute.Substitute.For<IHttpClientFactory>();
        factory.CreateClient("AiGateway").Returns(gatewayClient);
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton(factory);
            });
        });
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<T> PostJsonAsync<T>(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var response = await PostJsonRawAsync(authenticated, path, body, idempotencyKey);
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBeOneOf(
            [HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.Accepted],
            responseBody);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<HttpResponseMessage> PostJsonRawAsync(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: ClientJsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        request.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        return await authenticated.Client.SendAsync(request);
    }

    private static async Task<T> PutJsonAsync<T>(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var response = await PutJsonRawAsync(authenticated, path, body, idempotencyKey);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ClientJsonOptions);
        return payload!.Data!;
    }

    private static async Task<HttpResponseMessage> PutJsonRawAsync(
        AuthenticatedClient authenticated,
        string path,
        object body,
        string? idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body, options: ClientJsonOptions)
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        request.Headers.Add("X-CSRF-TOKEN", authenticated.CsrfToken);
        return await authenticated.Client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/v1/auth/csrf"));
        return document.RootElement.GetProperty("data").GetProperty("requestToken").GetString()
            ?? throw new InvalidOperationException("CSRF token is missing.");
    }

    private static IEnumerable<DepartmentTreeNodeDto> Flatten(DepartmentTreeNodeDto node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static IEnumerable<DepartmentTreeNodeDto> Flatten(
        IEnumerable<DepartmentTreeNodeDto> nodes) =>
        nodes.SelectMany(Flatten);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }

    private sealed record AuthenticatedClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed class RuleGatewayHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"id":"test-rule-call","audit":{"mode":"RuleSimulator","externalModelCalled":false,"outputDigest":"TEST-DIGEST"}}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            });
    }
}
