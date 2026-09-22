using EvidenceChain.Domain.Entities;
using EvidenceChain.Domain.Exceptions;
using EvidenceChain.Infrastructure;
using EvidenceChain.Infrastructure.Queries;
using EvidenceChain.Infrastructure.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvidenceChain.Tests
{
    [Collection("Database collection")]
    public class ConcurrencyTests : IDisposable
    {
        private readonly EvidenceChainDbContext _db;
        private readonly IDbContextTransaction _transaction;

        public ConcurrencyTests(DatabaseFixture fixture)
        {
            var options = new DbContextOptionsBuilder<EvidenceChainDbContext>()
                .UseSqlServer(fixture.ConnectionString)
                .Options;
            _db = new EvidenceChainDbContext(options);
            _transaction = _db.Database.BeginTransaction();
        }

        public void Dispose()
        {
            _transaction.Rollback();
            _db.Dispose();
        }

        [Fact]
        public async Task Accept_WithStaleETag_ThrowsConcurrencyConflict()
        {
            var custodioA = new Custodian { Id = Guid.NewGuid(), DisplayName = "A", Role = CustodianRole.Custodio, LoginCode = $"a-{Guid.NewGuid()}" };
            var custodioB = new Custodian { Id = Guid.NewGuid(), DisplayName = "B", Role = CustodianRole.Custodio, LoginCode = $"b-{Guid.NewGuid()}" };
            var evidence = new Evidence { Id = Guid.NewGuid(), Code = "EV-TEST-CONC", Description = "Test", CreatedAtUtc = DateTime.UtcNow, CurrentCustodianId = custodioA.Id };
            var transfer = new CustodyTransfer
            {
                Id = Guid.NewGuid(),
                EvidenceId = evidence.Id,
                FromCustodianId = custodioA.Id,
                ToCustodianId = custodioB.Id,
                Status = TransferStatus.Pending,
                RequestedAtUtc = DateTime.UtcNow
            };

            _db.Custodians.AddRange(custodioA, custodioB);
            _db.Evidences.Add(evidence);
            _db.CustodyTransfers.Add(transfer);
            await _db.SaveChangesAsync();

            var staleETag = $"\"{Convert.ToBase64String(transfer.RowVersion)}\"";

            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE CustodyTransfers SET RequestedAtUtc = RequestedAtUtc WHERE Id = {0}", transfer.Id);

            var sut = new CustodyTransferService(_db, new ChainVerification(_db));

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                sut.AcceptAsync(transfer.Id, staleETag, custodioB.Id));
        }
    }
}
