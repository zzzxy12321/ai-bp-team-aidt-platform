using AIDT.Application.Abstractions;
using AIDT.Domain.Identity;

namespace AIDT.Api.Identity;

public sealed class AnonymousCurrentUserAccessor : ICurrentUserAccessor
{
    public UserPrincipal GetRequiredUser() =>
        new(
            Guid.Empty,
            Guid.Empty,
            false,
            new HashSet<string>(StringComparer.Ordinal),
            [],
            []);
}
