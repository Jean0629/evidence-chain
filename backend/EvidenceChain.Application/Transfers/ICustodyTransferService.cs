using EvidenceChain.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Application.Transfers
{
    public interface ICustodyTransferService
    {
        Task<TransferResponseDto> CreateAsync(CreateTransferRequestDto request, string idempotencyKey, Guid requestedByCustodianId);
        Task<TransferResponseDto> AcceptAsync(Guid transferId, string ifMatchETag, Guid currentUserId);
        Task<TransferResponseDto> RejectAsync(Guid transferId, string ifMatchETag, Guid currentUserId);
        Task<List<MyPendingTransferDto>> GetPendingForCustodianAsync(Guid custodianId);
    }
}
