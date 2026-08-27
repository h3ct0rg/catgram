using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Auth;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Users;

public sealed class UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IAuditService audit) : IUserService
{
    public async Task<IReadOnlyCollection<UserSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(x => x.UserName).ToListAsync(cancellationToken);
        var result = new List<UserSummary>();
        foreach (var user in users) result.Add(new(user.Id, user.UserName ?? "", user.Email ?? "", user.FullName, user.IsActive, [.. await userManager.GetRolesAsync(user)]));
        return result;
    }

    public async Task SetActiveAsync(Guid userId, bool active, Guid actorUserId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        user.IsActive = active;
        await EnsureSuccess(userManager.UpdateAsync(user));
        await audit.RecordAsync(actorUserId, active ? AuditAction.UserActivated : AuditAction.UserDeactivated, "User", userId, null, cancellationToken);
    }

    public async Task AssignRoleAsync(Guid userId, string role, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(role)) throw new ArgumentException("Rol no válido.");
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Usuario no encontrado.");
        var current = await userManager.GetRolesAsync(user);
        await EnsureSuccess(userManager.RemoveFromRolesAsync(user, current));
        await EnsureSuccess(userManager.AddToRoleAsync(user, role));
        await audit.RecordAsync(actorUserId, AuditAction.UserRoleChanged, "User", userId, $"{string.Join(",", current)} -> {role}", cancellationToken);
    }

    private static async Task EnsureSuccess(Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
