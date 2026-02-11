namespace TrainingApp.Api.Contracts;

public record CreateCheckoutRequest(string Tier, string Interval, string SuccessUrl, string CancelUrl);
public record CheckoutSessionResponse(string SessionId, string SessionUrl);
public record CreatePortalRequest(string ReturnUrl);
public record PortalSessionResponse(string PortalUrl);
