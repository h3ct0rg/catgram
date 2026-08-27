using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Moderation;
using KindredPaws.Api.Infrastructure.Persistence;

namespace KindredPaws.Api.Application.Moderation;

public sealed class ReportService(ReportRepository repository, SocialRepository posts, CommentRepository comments, IAuditService audit) : IReportService
{
    public async Task<ReportResponse> CreateAsync(Guid reporterId, CreateReportRequest r, CancellationToken ct)
    {
        var reason = r.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("El motivo del reporte es obligatorio.");

        var targetExists = r.TargetType switch
        {
            ReportTargetType.Post => await posts.GetPostAsync(r.TargetId, ct) is not null,
            ReportTargetType.Comment => await comments.GetAsync(r.TargetId, ct) is not null,
            ReportTargetType.User => await repository.UserExistsAsync(r.TargetId, ct),
            _ => false,
        };
        if (!targetExists) throw new KeyNotFoundException("El contenido o usuario reportado no existe.");

        var report = new Report { ReporterId = reporterId, TargetType = r.TargetType, TargetId = r.TargetId, Reason = reason };
        await repository.AddAsync(report, ct);
        await repository.SaveAsync(ct);
        return ToResponse(report);
    }

    public async Task<IReadOnlyCollection<ReportResponse>> ListAsync(ReportStatus? status, ReportTargetType? targetType, CancellationToken ct) =>
        (await repository.ListAsync(status, targetType, ct)).Select(ToResponse).ToArray();

    public async Task<ReportResponse> ResolveAsync(Guid reportId, ReportStatus status, Guid actorUserId, CancellationToken ct)
    {
        var report = await repository.GetAsync(reportId, ct) ?? throw new KeyNotFoundException("Reporte no encontrado.");
        report.Status = status;
        await repository.SaveAsync(ct);
        await audit.RecordAsync(actorUserId, AuditAction.ReportResolved, "Report", reportId, $"{report.TargetType}:{report.TargetId} -> {status}", ct);
        return ToResponse(report);
    }

    private static ReportResponse ToResponse(Report x) => new(x.Id, x.ReporterId, x.TargetType, x.TargetId, x.Reason, x.Status, x.CreatedAt);
}
