namespace TrainingApp.Api.Contracts;

public record CreateInviteResponse(Guid PartnershipId, string InviteCode, string ExpiresAt);
public record AcceptInviteRequest(string InviteCode);
public record DeclineInviteRequest(string InviteCode);
public record PartnershipResponse(Guid Id, Guid RequesterId, string RequesterName,
    Guid? ResponderId, string? ResponderName, string Status,
    string? InviteCode, string? ExpiresAt, DateTimeOffset CreatedAt);
public record PartnershipListResponse(List<PartnershipResponse> Items, int TotalCount);
