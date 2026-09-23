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
                : query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id);

            var items = await query
                .Take(pageSize)
                .Select(x => new EvidenceListItemDto(x.Id, x.Code, x.Description, x.CurrentCustodian.DisplayName, x.CreatedAtUtc, false, false))
                .ToListAsync();

            var evidenceIds = items.Select(i => i.Id).ToList();

            var allEvents = await db.CustodyEvents
                .Where(e => evidenceIds.Contains(e.EvidenceId))
                .OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.Id)
                .ToListAsync();

            var eventsByEvidence = allEvents.GroupBy(e => e.EvidenceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var itemsWithIntegrity = items.Select(item =>
            {
                var events = eventsByEvidence.GetValueOrDefault(item.Id, new List<CustodyEvent>());
                var verify = ChainVerification.VerifyEvents(events);
                return item with { IsIntegrityValid = verify.IsValid };
            }).ToList();

            string? nextCursor = items.Count == pageSize
                ? $"{items[^1].LastEventAtUtc:O}_{items[^1].Id}"
                : null;

            return new EvidencePageDto(itemsWithIntegrity, nextCursor);
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
            var cutoff = DateTime.UtcNow.AddHours(-expirationHours);

            var overdueTransfer = await db.CustodyTransfers
                .Where(t => t.EvidenceId == id
                         && t.Status == TransferStatus.Pending
                         && t.RequestedAtUtc < cutoff)
                .OrderBy(t => t.RequestedAtUtc)
                .FirstOrDefaultAsync();

            bool hasAnomaly = overdueTransfer is not null;
            string? severity = null;
            string? reason = null;

            if (hasAnomaly)
            {
                var overdue = DateTime.UtcNow - overdueTransfer!.RequestedAtUtc;
                severity = CalculateSeverity(overdue, expirationHours);
                reason = $"Transferencia sin respuesta desde hace {overdue.Days} días (plazo: {expirationHours}h).";
            }

            var pending = await db.CustodyTransfers
                .Include(t => t.ToCustodian)
                .Where(t => t.EvidenceId == id && t.Status == TransferStatus.Pending)
                .OrderByDescending(t => t.RequestedAtUtc)
                .FirstOrDefaultAsync();

            var pendingDto = pending is null
                ? null
                : new PendingTransferDto(
                    pending.Id, pending.ToCustodianId, pending.ToCustodian.DisplayName, pending.RequestedAtUtc,
                    $"\"{Convert.ToBase64String(pending.RowVersion)}\"");

            return new EvidenceDetailDto(
                evidence.Id, evidence.Code, evidence.Description, evidence.CurrentCustodianId, evidence.CurrentCustodian.DisplayName,
                evidence.CreatedAtUtc, hasAnomaly, severity, reason, pendingDto);
        }

        public async Task<List<CustodyEventDto>> GetChainAsync(Guid evidenceId)
        {
            return await db.CustodyEvents
                .Include(x => x.Actor)
                .Include(x => x.RelatedTransfer)
                .ThenInclude(t => t!.ToCustodian)
                .Where(x => x.EvidenceId == evidenceId)
                .OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id)
                .Select(x => new CustodyEventDto(x.Id, x.Type.ToString(), x.Actor.DisplayName, x.OccurredAtUtc, x.Hash, x.PreviousHash, x.Type == CustodyEventType.TransferRequested ? x.RelatedTransfer!.ToCustodian.DisplayName : null, x.Type == CustodyEventType.TransferRequested ? x.RelatedTransfer!.FromCustodian.DisplayName : null))
                .ToListAsync();
        }
    }
}
