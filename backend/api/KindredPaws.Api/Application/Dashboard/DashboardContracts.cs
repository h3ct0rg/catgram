namespace KindredPaws.Api.Application.Dashboard;

public sealed record ShelterBreakdownItem(Guid ShelterId, string ShelterName, int AnimalCount, int AdoptedCount);
public sealed record AnimalEngagementItem(Guid AnimalId, string AnimalName, string ShelterName, int Likes, int Shares, int Views);

public sealed record DashboardSummaryResponse(
    int Users,
    int Shelters,
    int Animals,
    int AdoptedAnimals,
    int Posts,
    int ActiveStories,
    int Likes,
    int Comments,
    int Shares,
    int Views,
    IReadOnlyCollection<ShelterBreakdownItem> SheltersBreakdown,
    IReadOnlyCollection<AnimalEngagementItem> TopAnimals);

public sealed record ShelterDashboardSummaryResponse(
    int Animals,
    int AdoptedAnimals,
    int Posts,
    int Likes,
    int Comments,
    int Shares,
    int Views,
    int PendingAdoptionRequests);
