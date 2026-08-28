using System.Text;
using KindredPaws.Api.Application.Auth;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Application.Users;
using KindredPaws.Api.Application.Social;
using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Application.Engagement;
using KindredPaws.Api.Application.Follows;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Moderation;
using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Dashboard;
using KindredPaws.Api.Application.Adoption;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Infrastructure.Identity;
using KindredPaws.Api.Infrastructure.Media;
using KindredPaws.Api.Infrastructure.Messaging;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Kestrel's default MaxRequestBodySize (~28.6 MB) is lower than the 50 MB per-file media cap this
// API enforces in code — without raising it, a multipart upload near that cap fails at the server
// level (before reaching any controller/validation code) instead of the friendly "too large" error.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 200 * 1024 * 1024);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection("Authentication:Google"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<ISocialService, SocialService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdoptionService, AdoptionService>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<AdoptionRequestRepository>();
builder.Services.AddScoped<ShelterRepository>();
builder.Services.AddScoped<AnimalRepository>();
builder.Services.AddScoped<SocialRepository>();
builder.Services.AddScoped<LikeRepository>();
builder.Services.AddScoped<FollowRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<CommentLikeRepository>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddSingleton<IThumbnailGenerator, ImageSharpThumbnailGenerator>();
builder.Services.AddSingleton<IMinioService, MinioService>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:5173"];

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

// UseExceptionHandler resets the response before writing the ProblemDetails body, which wipes out
// any CORS headers UseCors had already set further down the pipeline. Applying the CORS policy again
// inside the error branch ensures a 500 still carries the right Access-Control-Allow-Origin header
// instead of surfacing to the browser as an opaque CORS failure.
app.UseExceptionHandler(errorApp =>
{
    errorApp.UseCors("Frontend");
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.RequestServices.GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = context });
    });
});
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Kindred Paws API v1"));
}

app.MapHealthChecks("/health");
app.MapControllers();
app.MapGet("/api/v1", () => Results.Ok(new
{
    name = "Kindred Paws API",
    version = "v1",
    status = "ready"
}));

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();

public partial class Program;
