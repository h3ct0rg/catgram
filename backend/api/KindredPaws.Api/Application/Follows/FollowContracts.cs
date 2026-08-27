namespace KindredPaws.Api.Application.Follows;

public sealed record FollowSummaryResponse(int FollowerCount, bool FollowedByCurrentUser);
