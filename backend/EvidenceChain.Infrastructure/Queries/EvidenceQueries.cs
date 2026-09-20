using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Evidence;
using EvidenceChain.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Infrastructure.Queries
{
    public class EvidenceQueries(EvidenceChainDbContext db, IConfiguration config) : IEvidenceQueries
    {
        public async Task<EvidencePageDto> GetPageAsync(string? search, Guid? custodianId, string? cursor, SortOrder order = SortOrder.Descending, int pageSize = 20)
        {
            var query = db.Evidences.Include(x => x.CurrentCustodian).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Code.Contains(search) || x.Description.Contains(search));

            if (custodianId.HasValue)
                query = query.Where(x => x.CurrentCustodianId == custodianId.Value);

            bool desc = order == SortOrder.Descending;

            if (!string.IsNullOrEmpty(cursor))
            {
                var parts = cursor.Split('_');
                var cursorDate = DateTime.Parse(parts[0]).ToUniversalTime();
                var cursorId = Guid.Parse(parts[1]);

                query = desc
                    ? query.Where(x => x.CreatedAtUtc < cursorDate || (x.CreatedAtUtc == cursorDate && x.Id.CompareTo(cursorId) < 0))
                    : query.Where(x => x.CreatedAtUtc > cursorDate || (x.CreatedAtUtc == cursorDate && x.Id.CompareTo(cursorId) > 0));
            }

            query = desc
                ? query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
                : query.OrderBy(x => x.CreatedAtUtc).OrderBy(x => x.Id);

            var items = await query
                .Take(pageSize)
                .Select(x => new EvidenceListItemDto(x.Id, x.Code, x.Description, x.CurrentCustodian.DisplayName, x.CreatedAtUtc, false))
                .ToListAsync();

            string? nextCursor = items.Count == pageSize
                ? $"{items[^1].LastEventAtUtc:O}_{items[^1].Id}"
                : null;

            return new EvidencePageDto(items, nextCursor);
        }

        private static string CalculateSeverity(TimeSpan overdue, int expirationHours)
        {
            var overdueRatio = overdue.TotalHours / expirationHours;

            return overdueRatio switch
            {
                < 2 => "low",
                < 4 => "medium",
                _ => "high"
            };
        }

        public async Task<EvidenceDetailDto?> GetByIdAsync(Guid id)
        {
            var evidence = await db.Evidences
                .Include(x => x.CurrentCustodian)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (evidence == null) return null;

            var expirationHours = config.GetValue<int>("TransferPolicy:ExpirationHours", 48);

            var overdueTransfer = await db.CustodyTransfers
                .Where(x => x.EvidenceId == id && x.Status == TransferStatus.Pending)
                .OrderBy(x => x.RequestedAtUtc)
                .FirstOrDefaultAsync(t => DateTime.UtcNow - t.RequestedAtUtc > TimeSpan.FromHours(expirationHours));

            bool hasAnomaly = overdueTransfer is not null;
            string? severity = null;
            string? reason = null;

            if (hasAnomaly)
            {
                var overdue = DateTime.UtcNow - overdueTransfer!.RequestedAtUtc;
                severity = CalculateSeverity(overdue, expirationHours);
                reason = $"Transferencia sin respuesta desde hace {overdue.Days} días (plazo: {expirationHours}h).";
            }

            return new EvidenceDetailDto(
            evidence.Id, evidence.Code, evidence.Description, evidence.CurrentCustodian.DisplayName,
            evidence.CreatedAtUtc, hasAnomaly, severity, reason);
        }

        public async Task<List<CustodyEventDto>> GetChainAsync(Guid evidenceId)
        {
            return await db.CustodyEvents
                .Include(x => x.Actor)
                .Where(x => x.EvidenceId == evidenceId)
                .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
                .Select(x => new CustodyEventDto(x.Id, x.Type.ToString(), x.Actor.DisplayName, x.OccurredAtUtc, x.Hash, x.PreviousHash))
                .ToListAsync();
        }
    }
}
