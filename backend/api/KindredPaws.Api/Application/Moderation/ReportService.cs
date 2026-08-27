using KindredPaws.Api.Domain.Moderation;
using KindredPaws.Api.Infrastructure.Persistence;

namespace KindredPaws.Api.Application.Moderation;

public sealed class ReportService(ReportRepository repository, SocialRepository posts, CommentRepository comments) : IReportService
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
        return new ReportResponse(report.Id, report.TargetType, report.TargetId, report.Reason, report.Status, report.CreatedAt);
    }
}
