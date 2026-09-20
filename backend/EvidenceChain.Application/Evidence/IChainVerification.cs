namespace EvidenceChain.Application.Evidence
{
    public record ChainVerifyResult(bool IsValid, Guid? FirstInvalidEventId);
    public interface IChainVerification
    {
        Task<ChainVerifyResult> VerifyAsync(Guid evidenceId);
    }
}
