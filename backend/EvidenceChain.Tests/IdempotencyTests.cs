using EvidenceChain.Application.DTOs;
using EvidenceChain.Domain.Entities;
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
    public class IdempotencyTests : IDisposable
    {
        private readonly EvidenceChainDbContext _db;
        private readonly IDbContextTransaction _transaction;

        public IdempotencyTests(DatabaseFixture fixture)
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
        public async Task CreateAsync_SameIdempotencyKey_DoesNotCreateDuplicateTransfer()
        {
            var investigador = new Custodian { Id = Guid.NewGuid(), DisplayName = "Inv", Role = CustodianRole.Investigador, LoginCode = $"inv-{Guid.NewGuid()}" };
            var custodioA = new Custodian { Id = Guid.NewGuid(), DisplayName = "A", Role = CustodianRole.Custodio, LoginCode = $"a-{Guid.NewGuid()}" };
            var custodioB = new Custodian { Id = Guid.NewGuid(), DisplayName = "B", Role = CustodianRole.Custodio, LoginCode = $"b-{Guid.NewGuid()}" };
            var evidence = new Evidence { Id = Guid.NewGuid(), Code = "EV-TEST-IDEM", Description = "Test", CreatedAtUtc = DateTime.UtcNow, CurrentCustodianId = custodioA.Id };

            _db.Custodians.AddRange(investigador, custodioA, custodioB);
            _db.Evidences.Add(evidence);
            await _db.SaveChangesAsync();

            var sut = new CustodyTransferService(_db, new ChainVerification(_db));
            var request = new CreateTransferRequestDto(evidence.Id, custodioB.Id);
            var idempotencyKey = Guid.NewGuid().ToString();

            var first = await sut.CreateAsync(request, idempotencyKey, investigador.Id);
            var second = await sut.CreateAsync(request, idempotencyKey, investigador.Id);

            Assert.Equal(first.Id, second.Id);

            var count = _db.CustodyTransfers.Count(t => t.EvidenceId == evidence.Id);
            Assert.Equal(1, count);
        }
    }
}
