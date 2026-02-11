namespace TrainingApp.Api.Contracts;

public record ChangeRoleRequest(string Role);
public record OverrideTierRequest(string Tier, string Reason);
public record ExtendTrialRequest(int Days);
public record AdminCancelRequest(string Reason);
