namespace KindredPaws.Api.Domain.Identity;

public static class Roles
{
    public const string Visitor = "Visitante";
    public const string User = "Usuario";
    public const string Administrator = "Administrador";
    public const string SuperAdministrator = "SuperAdministrador";

    public static readonly string[] All = [Visitor, User, Administrator, SuperAdministrator];
}
