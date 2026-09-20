namespace EvidenceChain.Application.Auth
{
    public record LoginRequest(string LoginCode);
    public record LoginResponse(string Token, string DisplayName, string Role);
}
