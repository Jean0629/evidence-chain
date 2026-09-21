using EvidenceChain.Application.DTOs;
using EvidenceChain.Application.Transfers;
using EvidenceChain.Domain.Entities;
using EvidenceChain.Domain.Exceptions;
using EvidenceChain.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EvidenceChain.Infrastructure.Transfers
{
    public class CustodyTransferService(EvidenceChainDbContext db) : ICustodyTransferService
    {
        public async Task<TransferResponseDto> CreateAsync(CreateTransferRequestDto request, string idempotencyKey, Guid requestedByCustodianId)
        {
            // -------------- 1. key ya procesada -------------------
            var existing = await db.IdempotencyKeys.FindAsync(idempotencyKey);
            if (existing is not null)
                return JsonSerializer.Deserialize<TransferResponseDto>(existing.ResponseBody)!;

            var evidence = await db.Evidences.FirstOrDefaultAsync(e => e.Id == request.EvidenceId)
                ?? throw new KeyNotFoundException("Evidencia no encontrada.");

            if (request.ToCustodianId == evidence.CurrentCustodianId)
                throw new InvalidOperationException("No se puede transferir la evidencia al mismo custodio que ya la tiene.");

            var transfer = new CustodyTransfer
            {
                Id = Guid.NewGuid(),
                EvidenceId = request.EvidenceId,
                FromCustodianId = evidence.CurrentCustodianId,
                ToCustodianId = request.ToCustodianId,
                Status = TransferStatus.Pending,
                RequestedAtUtc = DateTime.UtcNow
            };
            db.CustodyTransfers.Add(transfer);

            var lastEvent = await db.CustodyEvents
                .Where(e => e.EvidenceId == request.EvidenceId)
                .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
                .FirstOrDefaultAsync();
            var prevHash = lastEvent?.Hash ?? EventHasher.Genesis;

            db.CustodyEvents.Add(CustodyEvent.Create(
                request.EvidenceId, CustodyEventType.TransferRequested, requestedByCustodianId, transfer.RequestedAtUtc, prevHash));

            await db.SaveChangesAsync();

            var dto = ToDto(transfer);

            // -------------- 2. Guardado de la respuesta asociada a la key -------------------
            db.IdempotencyKeys.Add(new IdempotencyKeyRecord
            {
                Key = idempotencyKey,
                ResponseBody = JsonSerializer.Serialize(dto),
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return dto;
        }

        public Task<TransferResponseDto> AcceptAsync(Guid transferId, string ifMatchETag, Guid currentUserId) =>
            ResolveAsync(transferId, ifMatchETag, currentUserId, t => t.Accept(), CustodyEventType.TransferAccepted);

        public Task<TransferResponseDto> RejectAsync(Guid transferId, string ifMatchETag, Guid currentUserId) =>
            ResolveAsync(transferId, ifMatchETag, currentUserId, t => t.Reject(), CustodyEventType.TransferRejected);

        public async Task<List<MyPendingTransferDto>> GetPendingForCustodianAsync(Guid custodianId)
        {
            return await db.CustodyTransfers
                .Include(t => t.Evidence)
                .Include(t => t.FromCustodian)
                .Where(t => t.ToCustodianId == custodianId && t.Status == TransferStatus.Pending)
                .OrderBy(t => t.RequestedAtUtc)
                .Select(t => new MyPendingTransferDto(
                    t.Id, t.EvidenceId, t.Evidence.Code, t.FromCustodian.DisplayName,
                    t.RequestedAtUtc, $"\"{Convert.ToBase64String(t.RowVersion)}\""))
                .ToListAsync();
        }

        private async Task<TransferResponseDto> ResolveAsync(Guid transferId, string ifMatchETag, Guid currentUserId, Action<CustodyTransfer> transition, CustodyEventType eventType)
        {
            var transfer = await db.CustodyTransfers.FirstOrDefaultAsync(t => t.Id == transferId)
                ?? throw new KeyNotFoundException("Transferencia no encontrada.");

            if (transfer.ToCustodianId != currentUserId)
                throw new ForbiddenTransferActionException();

            db.Entry(transfer).Property(t => t.RowVersion).OriginalValue = DecodeETag(ifMatchETag);

            try
            {
                transition(transfer);

                if (transfer.Status == TransferStatus.Accepted)
                {
                    var evidence = await db.Evidences.FirstAsync(e => e.Id == transfer.EvidenceId);
                    evidence.CurrentCustodianId = transfer.ToCustodianId;
                }

                var lastEvent = await db.CustodyEvents
                    .Where(e => e.EvidenceId == transfer.EvidenceId)
                    .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
                    .FirstOrDefaultAsync();
                var prevHash = lastEvent?.Hash ?? EventHasher.Genesis;
                var actorId = transfer.ToCustodianId;

                db.CustodyEvents.Add(CustodyEvent.Create(transfer.EvidenceId, eventType, actorId, DateTime.UtcNow, prevHash));

                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var currentStatus = await db.CustodyTransfers
                    .Where(t => t.Id == transferId)
                    .Select(t => t.Status)
                    .FirstAsync();
                throw new ConcurrencyConflictException(currentStatus.ToString());
            }

            return ToDto(transfer);
        }

        private static byte[] DecodeETag(string etag) => Convert.FromBase64String(etag.Trim('"'));

        private static TransferResponseDto ToDto(CustodyTransfer t) => new(
            t.Id, t.EvidenceId, t.Status.ToString(), t.FromCustodianId, t.ToCustodianId, t.RequestedAtUtc,
            $"\"{Convert.ToBase64String(t.RowVersion)}\"");
    }
}
