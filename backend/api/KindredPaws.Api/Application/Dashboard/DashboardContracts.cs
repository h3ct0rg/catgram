namespace KindredPaws.Api.Application.Dashboard;

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
    int Views);
