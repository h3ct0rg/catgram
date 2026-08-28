using System.Text.Json;
using KindredPaws.Api.Application.Animals;
using KindredPaws.Api.Application.Audit;
using KindredPaws.Api.Application.Notifications;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Api.Domain.Adoption;
using KindredPaws.Api.Domain.Animals;
using KindredPaws.Api.Domain.Audit;
using KindredPaws.Api.Domain.Identity;
using KindredPaws.Api.Domain.Notifications;
using KindredPaws.Api.Infrastructure.Persistence;
using KindredPaws.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KindredPaws.Api.Application.Adoption;

public sealed class AdoptionService(
    AdoptionRequestRepository requests,
    AnimalRepository animals,
    IAnimalService animalService,
    UserManager<ApplicationUser> userManager,
    INotificationService notifications,
    IEventPublisher eventPublisher,
    IAuditService audit) : IAdoptionService
{
    public async Task<AdoptionRequestResponse> CreateAsync(Guid animalId, Guid applicantUserId, CreateAdoptionRequestRequest r, CancellationToken ct)
    {
        var animal = await animals.GetAsync(animalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        var request = new AdoptionRequest { AnimalId = animalId, ApplicantUserId = applicantUserId, AnswersJson = JsonSerializer.Serialize(r.Answers) };
        await requests.AddAsync(request, ct);
        await requests.SaveAsync(ct);
        await NotifyShelterAdminsAsync(animal, request, ct);
        return await ToResponseAsync(request, animal.Name, ct);
    }

    public async Task<IReadOnlyCollection<AdoptionRequestResponse>> ListAsync(AdoptionRequestStatus? status, Guid? animalId, Guid? actorShelterId, CancellationToken ct) =>
        await ToResponsesAsync(await requests.ListAsync(status, animalId, actorShelterId, ct), ct);

    public async Task<IReadOnlyCollection<AdoptionRequestResponse>> ListMineAsync(Guid applicantUserId, CancellationToken ct) =>
        await ToResponsesAsync(await requests.ListByApplicantAsync(applicantUserId, ct), ct);

    public async Task<AdoptionRequestResponse> UpdateStatusAsync(Guid id, AdoptionRequestStatus status, string? reviewNotes, Guid actorUserId, Guid? actorShelterId, CancellationToken ct)
    {
        var request = await requests.GetAsync(id, ct) ?? throw new KeyNotFoundException("Solicitud no encontrada.");
        var animal = await animals.GetAsync(request.AnimalId, ct) ?? throw new KeyNotFoundException("Animal no encontrado.");
        if (actorShelterId.HasValue && animal.ShelterId != actorShelterId.Value)
            throw new UnauthorizedAccessException("No puedes revisar solicitudes de otro refugio.");

        request.Status = status;
        request.ReviewNotes = reviewNotes;
        request.ReviewedByUserId = actorUserId;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        await requests.SaveAsync(ct);

        await audit.RecordAsync(actorUserId, AuditAction.AdoptionRequestReviewed, "AdoptionRequest", id, $"{animal.Name} -> {status}", ct);

        if (status == AdoptionRequestStatus.Completed) await animalService.MarkAdoptedAsync(request.AnimalId, actorUserId, ct);

        var applicant = await userManager.FindByIdAsync(request.ApplicantUserId.ToString());
        if (applicant is not null)
        {
            await notifications.CreateAsync(applicant.Id, NotificationType.AdoptionRequestStatusChanged, "Actualización de tu solicitud de adopción", $"Tu solicitud para {animal.Name} cambió a {status}.", $"/animals/{animal.Id}", request.Id, ct);
            await eventPublisher.PublishAsync(new AdoptionRequestStatusChangedEvent(request.Id, animal.Id, animal.Name, status.ToString(), applicant.Id, applicant.Email ?? string.Empty, applicant.FullName), ct);
        }

        return await ToResponseAsync(request, animal.Name, ct);
    }

    private async Task NotifyShelterAdminsAsync(Animal animal, AdoptionRequest request, CancellationToken ct)
    {
        var applicant = await userManager.FindByIdAsync(request.ApplicantUserId.ToString());
        var applicantName = applicant?.FullName ?? "Un usuario";
        var admins = await userManager.Users.Where(u => u.ShelterId == animal.ShelterId).ToListAsync(ct);
        foreach (var admin in admins)
        {
            await notifications.CreateAsync(admin.Id, NotificationType.NewAdoptionRequest, "Nueva solicitud de adopción", $"{applicantName} quiere adoptar a {animal.Name}.", $"/admin/adoptions", request.Id, ct);
            await eventPublisher.PublishAsync(new NewAdoptionRequestEvent(request.Id, animal.Id, animal.Name, applicantName, admin.Id, admin.Email ?? string.Empty, admin.FullName), ct);
        }
    }

    private async Task<IReadOnlyCollection<AdoptionRequestResponse>> ToResponsesAsync(IReadOnlyCollection<AdoptionRequest> list, CancellationToken ct)
    {
        if (list.Count == 0) return [];
        var animalNames = new Dictionary<Guid, string>();
        foreach (var animalId in list.Select(x => x.AnimalId).Distinct())
        {
            var animal = await animals.GetAsync(animalId, ct);
            if (animal is not null) animalNames[animalId] = animal.Name;
        }
        var result = new List<AdoptionRequestResponse>();
        foreach (var request in list) result.Add(await ToResponseAsync(request, animalNames.GetValueOrDefault(request.AnimalId, ""), ct));
        return result;
    }

    private async Task<AdoptionRequestResponse> ToResponseAsync(AdoptionRequest x, string animalName, CancellationToken ct)
    {
        var applicant = await userManager.FindByIdAsync(x.ApplicantUserId.ToString());
        var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(x.AnswersJson) ?? [];
        return new(x.Id, x.AnimalId, animalName, x.ApplicantUserId, applicant?.UserName ?? "desconocido", x.Status, answers, x.ReviewNotes, x.CreatedAt, x.UpdatedAt);
    }
}
