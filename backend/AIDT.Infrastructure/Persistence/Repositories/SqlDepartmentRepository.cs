using AIDT.Application.Abstractions;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlDepartmentRepository(AidtDbContext dbContext) : IDepartmentRepository
{
    public async Task<Guid> ResolveIdAsync(
        UserPrincipal user,
        Guid? requestedDepartmentId,
        string objectTypeCode,
        CancellationToken cancellationToken)
    {
        if (requestedDepartmentId.HasValue)
        {
            user.EnsureDepartmentAccess(requestedDepartmentId.Value);
            var exists = await dbContext.Departments.AsNoTracking().AnyAsync(
                item => item.Id == requestedDepartmentId.Value
                    && item.OrganizationId == user.OrganizationId
                    && item.IsActive,
                cancellationToken);
            return exists
                ? requestedDepartmentId.Value
                : throw new NotFoundException("部门不存在或已停用。");
        }

        if (!user.IsPlatformAdmin)
        {
            var preferred = await (
                from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                join departmentRow in dbContext.Departments.AsNoTracking()
                    on membership.DepartmentId equals departmentRow.Id
                where membership.UserId == user.UserId
                    && membership.IsActive
                    && departmentRow.OrganizationId == user.OrganizationId
                    && departmentRow.IsActive
                orderby membership.IsPrimary descending, membership.JoinedAt
                select (Guid?)membership.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);
            return preferred ?? throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前身份没有可用部门，请先配置部门成员关系。");
        }

        var departmentCode = objectTypeCode switch
        {
            "CUSTOMER_SAMPLE" => "RND",
            "QUALITY_DEVIATION" => "QA",
            "PURCHASE_REQUEST" => "SUPPLY",
            "CUSTOMER_COMPLAINT" => "QA",
            _ => null
        };
        var query = dbContext.Departments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId && item.IsActive);
        var department = departmentCode is null
            ? await query.OrderBy(item => item.Code).FirstOrDefaultAsync(cancellationToken)
            : await query.OrderBy(item => item.Code == departmentCode ? 0 : 1)
                .FirstOrDefaultAsync(cancellationToken);
        return department?.Id ?? throw new ValidationException("当前组织没有可用部门。");
    }

    public async Task<Guid> ResolveAssigneeDepartmentAsync(
        Guid organizationId,
        Guid assigneeUserId,
        CancellationToken cancellationToken)
    {
        var departmentId = await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join department in dbContext.Departments.AsNoTracking()
                on membership.DepartmentId equals department.Id
            where membership.UserId == assigneeUserId
                && membership.IsActive
                && department.OrganizationId == organizationId
                && department.IsActive
            orderby membership.IsPrimary descending, membership.JoinedAt
            select (Guid?)department.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return departmentId ?? throw new ValidationException("任务处理人没有可用部门。");
    }
}
