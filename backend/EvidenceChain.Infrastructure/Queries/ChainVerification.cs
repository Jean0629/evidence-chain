using EvidenceChain.Application.Evidence;
using EvidenceChain.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace EvidenceChain.Infrastructure.Queries
{
    public class ChainVerification(EvidenceChainDbContext db) : IChainVerification
    {
        public async Task<ChainVerifyResult> VerifyAsync(Guid evidenceId)
        {
            var events = await db.CustodyEvents
                .Where(x => x.EvidenceId == evidenceId)
                .OrderBy(x => x.OccurredAtUtc)
                .ThenBy(x => x.Id)
                .ToListAsync();

            string prevHash = EventHasher.Genesis;

            foreach (var ev in events)
            {
                var expected = EventHasher.ComputeHash(ev.EvidenceId, ev.Type.ToString(), ev.ActorId, ev.OccurredAtUtc, prevHash);
                if (expected != ev.Hash)
                    return new ChainVerifyResult(false, ev.Id);

                prevHash = ev.Hash;
            }

            return new ChainVerifyResult(true, null);
        }
    }
}
