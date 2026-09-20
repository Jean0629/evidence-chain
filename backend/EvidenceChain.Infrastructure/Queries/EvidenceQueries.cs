using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Evidence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Infrastructure.Queries
{
    public class EvidenceQueries(EvidenceChainDbContext db) : IEvidenceQueries
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

        public async Task<EvidenceDetailDto?> GetByIdAsync(Guid id)
        {
            return await db.Evidences
                .Include(x => x.CurrentCustodian)
                .Where(x => x.Id == id)
                .Select(x => new EvidenceDetailDto(x.Id, x.Code, x.Description, x.CurrentCustodian.DisplayName, x.CreatedAtUtc))
                .FirstOrDefaultAsync();
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
