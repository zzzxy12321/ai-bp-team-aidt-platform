using AIDT.Domain.Common;

namespace AIDT.Domain.Identity;

public sealed class Department
{
    private Department(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        Guid? managerUserId,
        bool isActive)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        ManagerUserId = managerUserId;
        IsActive = isActive;
    }

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public Guid? ManagerUserId { get; }
    public bool IsActive { get; }

    public static Department Create(
        Guid organizationId,
        string code,
        string name,
        Guid? managerUserId,
        bool isActive = true) =>
        Restore(Guid.NewGuid(), organizationId, code, name, managerUserId, isActive);

    public static Department Restore(
        Guid id,
        Guid organizationId,
        string code,
        string name,
        Guid? managerUserId,
        bool isActive) =>
        new(
            DomainChecks.RequiredId(id, "部门"),
            DomainChecks.RequiredId(organizationId, "组织"),
            DomainChecks.RequiredCode(code, "部门编码", 64),
            DomainChecks.Required(name, "部门名称", 128),
            managerUserId,
            isActive);
}

public sealed record DepartmentMembership(
    Guid UserId,
    Guid DepartmentId,
    bool IsPrimary,
    bool IsManager,
    bool IsActive,
    DateTimeOffset JoinedAt);
