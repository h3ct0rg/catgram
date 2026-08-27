using KindredPaws.Api.Application.Auth;

namespace KindredPaws.Api.Application.Users;

public interface IUserService
{
    Task<IReadOnlyCollection<UserSummary>> ListAsync(CancellationToken cancellationToken);
    Task SetActiveAsync(Guid userId, bool active, Guid actorUserId, CancellationToken cancellationToken);
    Task AssignRoleAsync(Guid userId, string role, Guid actorUserId, CancellationToken cancellationToken);
}
