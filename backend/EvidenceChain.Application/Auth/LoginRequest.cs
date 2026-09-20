namespace EvidenceChain.Application.Auth
{
    public record LoginRequest(string LoginCode);
    public record LoginResponse(string Token, Guid CustodianId, string DisplayName, string Role);
}
