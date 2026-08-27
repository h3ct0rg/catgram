using KindredPaws.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace KindredPaws.Api.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));

        var username = configuration["Seed:SuperAdmin:UserName"] ?? "superadmin";
        var password = configuration["Seed:SuperAdmin:Password"] ?? "superadmin";
        var admin = await userManager.FindByNameAsync(username);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = username,
                Email = configuration["Seed:SuperAdmin:Email"] ?? "superadmin@kindredpaws.local",
                FullName = "Super Administrador",
                MustChangePassword = true,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.SuperAdministrator))
            await userManager.AddToRoleAsync(admin, Roles.SuperAdministrator);
    }
}
